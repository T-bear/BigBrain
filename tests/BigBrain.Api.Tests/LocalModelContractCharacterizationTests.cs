using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BigBrain.Brain;
using BigBrain.Modules.Finance;

namespace BigBrain.Api.Tests;

// Source-driven version-binding regressions, not inference or a replacement GBNF/Finance parser.
// Only the current literal + identity/text subset is expanded; changed syntax fails this test helper.
public sealed class LocalModelContractCharacterizationTests
{
    private static readonly string[] IdentityFields = ["inputChecksum", "scopeHandle"];
    private static readonly string[] DeclineTextFields = ["explanation"];
    private static readonly string[] ProposalTextFields = ["question", "rationale"];
    private static string WorkerSource => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
        "contract-characterization", "worker.cpp"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactProjectedVersionAndExistingFixtureAreRepresentableByCurrentGrammar(bool decline)
    {
        var scope = ResearchLearningFixture.Scope();
        var bytes = LocalReasonerProtocol.Request(scope.Input);
        var input = LocalReasonerProtocol.ReadRequest(await FrameRoundTrip(bytes));
        using var projection = JsonDocument.Parse(bytes);
        Assert.Equal("finance-research-learning-v1", LearningAdmissionPolicy.Version);
        Assert.Equal(LearningAdmissionPolicy.Version, projection.RootElement.GetProperty("version").GetString());
        Assert.Equal(scope.Input, input);
        Assert.NotEqual(input.Version, input.ProjectionVersion);
        Assert.Contains("Copy version, inputChecksum, scopeHandle and targetId exactly from Finance input.", WorkerSource);
        Assert.Contains("\"version\":\"COPY\"", WorkerSource);

        var fixture = Fixture(scope, decline);
        var wire = GrammarWitness(fixture, decline);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(fixture), JsonNode.Parse(wire)));
        var parsed = await ParseFramed(wire, input);
        Assert.Null(parsed.Rejection);
        if (decline) Assert.IsType<LearningReasonerReply.NoUsefulProposal>(parsed.Reply);
        else Assert.IsType<LearningReasonerReply.Proposal>(parsed.Reply);
        // Pure admission only: no persistent ledger, reservation, evaluator or model is invoked.
        Assert.Equal(decline ? LearningAdmissionReason.NoUsefulProposal : LearningAdmissionReason.Admitted,
            LearningAdmissionPolicy.Admit(wire, scope, ResearchLearningFixture.Fresh).Reason);
    }

    [Theory]
    [InlineData(false, "v1")]
    [InlineData(true, "v1")]
    [InlineData(false, "development-only-v1")]
    [InlineData(true, "development-only-v1")]
    [InlineData(false, "COPY")]
    [InlineData(true, "COPY")]
    [InlineData(false, "finance-research-learning-v2")]
    [InlineData(true, "finance-research-learning-v2")]
    [InlineData(false, "FINANCE-RESEARCH-LEARNING-V1")]
    [InlineData(true, "FINANCE-RESEARCH-LEARNING-V1")]
    public async Task GrammarPinsFinanceVersionAndFinanceStillRejectsWrongVersionsWithoutCoercion(bool decline, string version)
    {
        var scope = ResearchLearningFixture.Scope();
        var node = JsonNode.Parse(Fixture(scope, decline))!.AsObject();
        node["version"] = version; // Deliberate synthetic witness, NOT a reconstructed RC03 response.
        var generated = GrammarWitness(node.ToJsonString(), decline);
        using var output = JsonDocument.Parse(generated);
        Assert.Equal(LearningAdmissionPolicy.Version, output.RootElement.GetProperty("version").GetString());
        Assert.NotEqual(version, output.RootElement.GetProperty("version").GetString());
        // Synthetic wrong-version wire bypasses grammar for the negative Finance control only.
        var wire = node.ToJsonString();
        var parsed = await ParseFramed(wire, scope.Input);
        Assert.Equal(LearningAdmissionReason.UnsupportedContract, parsed.Rejection);
        Assert.Null(parsed.Reply);
        Assert.Equal(LearningAdmissionReason.UnsupportedContract,
            LearningAdmissionPolicy.Admit(wire, scope, ResearchLearningFixture.Fresh).Reason);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownDiscriminatorIsAnotherParserPathButNotAProductionOfCurrentGrammar(bool decline)
    {
        var scope = ResearchLearningFixture.Scope();
        var wire = GrammarWitness(Fixture(scope, decline), decline);
        var node = JsonNode.Parse(wire)!.AsObject();
        Assert.Equal(decline ? "NoUsefulProposal" : "Proposal", node["discriminator"]!.GetValue<string>());
        node["discriminator"] = "ToolCall";
        var parsed = await ParseFramed(node.ToJsonString(), scope.Input);
        Assert.Equal(LearningAdmissionReason.UnsupportedContract, parsed.Rejection);
        Assert.Null(parsed.Reply);
        // The expander has no discriminator argument: both values come from literal grammar terminals.
        Assert.Equal("proposal | decline", Rules()["root"]);
    }

    private static string Fixture(SyntheticLearningScope scope, bool decline) =>
        decline ? ResearchLearningFixture.NoUseful(scope) : ResearchLearningFixture.Proposal(scope);

    private static async Task<LearningReplyParseResult> ParseFramed(string wire, LearningDevelopmentInput input) =>
        LearningReplyParser.Parse(LocalReasonerProtocol.Decode(await FrameRoundTrip(Encoding.UTF8.GetBytes(wire))), input);

    private static async Task<byte[]> FrameRoundTrip(byte[] bytes)
    {
        using var stream = new MemoryStream();
        await LocalReasonerProtocol.WriteAsync(stream, bytes, CancellationToken.None);
        stream.Position = 0;
        return await LocalReasonerProtocol.ReadAsync(stream, CancellationToken.None);
    }

    private static Dictionary<string, string> Rules()
    {
        var source = WorkerSource;
        const string start = "R\"gbnf(\n";
        var offset = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(offset >= 0);
        offset += start.Length;
        var end = source.IndexOf(")gbnf\";", offset, StringComparison.Ordinal);
        Assert.True(end > offset);
        return source[offset..end].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split(" ::= ", 2)).ToDictionary(x => x[0], x => x[1]);
    }

    private static string GrammarWitness(string fixture, bool decline)
    {
        var rules = Rules();
        Assert.Equal(5, rules.Count);
        Assert.Equal("proposal | decline", rules["root"]);
        Assert.Equal("\"\\\"\" [a-zA-Z0-9_./:-]{1,128} \"\\\"\"", rules["identity"]);
        Assert.Equal("\"\\\"\" [a-zA-Z0-9 .,?;:()-]{1,96} \"\\\"\"", rules["text"]);
        using var doc = JsonDocument.Parse(fixture);
        var root = doc.RootElement;
        var identities = new Queue<string>(IdentityFields
            .Select(x => root.GetProperty(x).GetString()!));
        if (!decline)
        {
            identities.Enqueue(root.GetProperty("targetId").GetString()!);
            identities.Enqueue(root.GetProperty("variants")[0].GetProperty("bindingHandle").GetString()!);
        }
        var prose = new Queue<string>((decline ? DeclineTextFields : ProposalTextFields)
            .Select(x => root.GetProperty(x).GetString()!));
        var rule = rules[decline ? "decline" : "proposal"];
        var output = new StringBuilder();
        var offset = 0;
        foreach (Match token in Regex.Matches(rule, "\"(?:\\\\.|[^\"\\\\])*\"|identity|text"))
        {
            Assert.True(string.IsNullOrWhiteSpace(rule[offset..token.Index]));
            if (token.Value.StartsWith('"')) output.Append(JsonSerializer.Deserialize<string>(token.Value));
            else
            {
                var value = token.Value == "identity" ? identities.Dequeue() : prose.Dequeue();
                // Exact ASCII character classes and bounds pinned above; this is not a general GBNF engine.
                Assert.Matches(token.Value == "identity" ? @"\A[a-zA-Z0-9_./:\-]{1,128}\z" : @"\A[a-zA-Z0-9 .,?;:()\-]{1,96}\z", value);
                output.Append('"').Append(value).Append('"');
            }
            offset = token.Index + token.Length;
        }
        Assert.True(string.IsNullOrWhiteSpace(rule[offset..]));
        Assert.Empty(identities); Assert.Empty(prose);
        return output.ToString();
    }
}
