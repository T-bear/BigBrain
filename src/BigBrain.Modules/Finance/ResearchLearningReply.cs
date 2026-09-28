using System.Text;
using System.Text.Json;

namespace BigBrain.Modules.Finance;

// Finance-owned parsing of the existing B wire schema, not scientific admission.
// A future transport must also bound bytes while receiving; this API receives a complete string.
public static class LearningReplyParser
{
    public static LearningReplyParseResult Parse(string response, LearningDevelopmentInput input) => Read(response, input, null);

    internal static LearningReplyParseResult Read(string response, LearningDevelopmentInput input,
        Func<string, LearningAdmissionReason?>? validateContext)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (response is null || Encoding.UTF8.GetByteCount(response) > 65536) return new(LearningAdmissionReason.Malformed);
        try
        {
            using var document = JsonDocument.Parse(response, new JsonDocumentOptions { MaxDepth = 8 });
            var root = document.RootElement;
            CheckUniqueKeys(root);
            var discriminator = Text(root, "discriminator", 32);
            if (discriminator == "NoUsefulProposal")
            {
                Shape(root, "version", "discriminator", "inputChecksum", "scopeHandle", "reasonCode", "explanation");
                if (Text(root, "reasonCode", 32) is not ("InsufficientEvidence" or "NoSupportedQuestion"))
                    return new(LearningAdmissionReason.Malformed);
                _ = Text(root, "explanation", 2048);
            }
            else if (discriminator == "Proposal")
                Shape(root, "version", "discriminator", "inputChecksum", "scopeHandle", "targetId", "question",
                    "rationale", "parentHypothesisRef", "variants", "falsificationCriteria");
            else return new(LearningAdmissionReason.UnsupportedContract);
            if (Text(root, "version", 80) != LearningAdmissionPolicy.Version) return new(LearningAdmissionReason.UnsupportedContract);
            if (Text(root, "inputChecksum", 80) != input.InputChecksum) return new(LearningAdmissionReason.EvidenceMismatch);
            if (Text(root, "scopeHandle", 80) != SyntheticLearningScope.Handle) return new(LearningAdmissionReason.UnsupportedScope);
            // Admission supplies this callback to preserve B's exact validation order.
            // Standalone parsing has no eligibility/budget/exposure authority.
            if (validateContext?.Invoke(discriminator) is { } rejection) return new(rejection);
            if (discriminator == "NoUsefulProposal")
                return new(null, new LearningReasonerReply.NoUsefulProposal(response,
                    Text(root, "reasonCode", 32), Text(root, "explanation", 2048)));
            if (Text(root, "targetId", 80) != input.TargetId) return new(LearningAdmissionReason.UnsupportedScope);
            if (root.GetProperty("parentHypothesisRef").ValueKind != JsonValueKind.Null) return new(LearningAdmissionReason.InvalidParent);
            var question = Text(root, "question", 1024);
            var rationale = Text(root, "rationale", 2048);
            var variants = root.GetProperty("variants");
            if (variants.ValueKind != JsonValueKind.Array || variants.GetArrayLength() != 1) return new(LearningAdmissionReason.InvalidVariantCount);
            var variant = variants[0];
            Shape(variant, "strategy", "parameters", "bindingHandle");
            var strategy = variant.GetProperty("strategy");
            Shape(strategy, "id", "version");
            if (Text(strategy, "id", 80) != "momentum" || Text(strategy, "version", 80) != "v1")
                return new(LearningAdmissionReason.UnsupportedStrategy);
            if (Text(variant, "bindingHandle", 80) != SyntheticLearningScope.Handle) return new(LearningAdmissionReason.UnsupportedScope);
            var parameters = variant.GetProperty("parameters");
            Shape(parameters, "period");
            if (parameters.GetProperty("period").GetDecimal() != 20m) return new(LearningAdmissionReason.UnsupportedParameter);
            var criteria = root.GetProperty("falsificationCriteria");
            if (criteria.ValueKind != JsonValueKind.Array || criteria.GetArrayLength() != 1) return new(LearningAdmissionReason.InvalidFalsification);
            var criterion = criteria[0];
            Shape(criterion, "metric", "phase", "comparison", "threshold", "unit", "samplePolicy");
            if (Text(criterion, "metric", 80) != LearningAdmissionPolicy.Criterion.Metric || Text(criterion, "phase", 80) != LearningAdmissionPolicy.Criterion.Phase ||
                Text(criterion, "comparison", 80) != nameof(LearningComparison.LessThanOrEqual) ||
                criterion.GetProperty("threshold").GetDecimal() != 0m || Text(criterion, "unit", 80) != LearningAdmissionPolicy.Criterion.Unit ||
                Text(criterion, "samplePolicy", 80) != LearningAdmissionPolicy.Criterion.SamplePolicy)
                return new(LearningAdmissionReason.InvalidFalsification);
            var draft = new LearningProposalDraft(question, rationale,
                new(new("momentum", "v1"), 20m, SyntheticLearningScope.Handle), LearningAdmissionPolicy.Criterion);
            return new(null, new LearningReasonerReply.Proposal(response, draft));
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { return new(LearningAdmissionReason.Malformed); }
    }

    private static void Shape(JsonElement element, params string[] properties)
    {
        if (element.ValueKind != JsonValueKind.Object || element.EnumerateObject().Count() != properties.Length ||
            element.EnumerateObject().Any(x => !properties.Contains(x.Name, StringComparer.Ordinal))) throw new JsonException("Closed shape required.");
    }
    private static string Text(JsonElement element, string name, int maximum)
    {
        var value = element.GetProperty(name).GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum) throw new JsonException("Bounded text required.");
        return value;
    }
    private static void CheckUniqueKeys(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            { if (!names.Add(property.Name)) throw new JsonException("Duplicate property."); CheckUniqueKeys(property.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CheckUniqueKeys(item);
    }
}

// Only the Finance parser constructs a reply. Its shape is validated, but it is still untrusted:
// Finance must re-admit the exact original response against current authoritative scope/state.
public abstract class LearningReasonerReply
{
    private LearningReasonerReply(string responseJson) => ResponseJson = responseJson;
    public string ResponseJson { get; }

    public sealed class Proposal : LearningReasonerReply
    {
        internal Proposal(string responseJson, LearningProposalDraft draft) : base(responseJson) => Draft = draft;
        public LearningProposalDraft Draft { get; }
    }

    public sealed class NoUsefulProposal : LearningReasonerReply
    {
        internal NoUsefulProposal(string responseJson, string reasonCode, string explanation) : base(responseJson)
        { ReasonCode = reasonCode; Explanation = explanation; }
        public string ReasonCode { get; }
        public string Explanation { get; }
    }
}

// Null rejection means parsed, never admitted, reserved, eligible or scientifically valid.
public sealed record LearningReplyParseResult(LearningAdmissionReason? Rejection, LearningReasonerReply? Reply = null);
