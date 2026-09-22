using System.Text;
using System.Text.Json;

namespace BigBrain.Modules.Finance;

// Pure admission: no engine, provider, persistence, clock or network access.
public static class LearningAdmissionPolicy
{
    public const string Version = "finance-research-learning-v1";
    public static LearningFalsification Criterion => new("validation.excessReturn", "validation",
        LearningComparison.LessThanOrEqual, 0m, "fraction", AntiOverfittingPolicy.Default.Version);

    public static LearningAdmission Admit(string response, SyntheticLearningScope scope, LearningAdmissionState state)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(state);
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
            if (Text(root, "version", 80) != Version) return new(LearningAdmissionReason.UnsupportedContract);
            if (Text(root, "inputChecksum", 80) != scope.Input.InputChecksum) return new(LearningAdmissionReason.EvidenceMismatch);
            if (Text(root, "scopeHandle", 80) != SyntheticLearningScope.Handle) return new(LearningAdmissionReason.UnsupportedScope);
            if (scope.ValidationReason != LearningAdmissionReason.Admitted) return new(scope.ValidationReason);
            if (state.SubmittedProposals < 0 || state.AdmittedTrials < 0 || state.Evaluations < 0)
                return new(LearningAdmissionReason.BudgetExceeded);
            if (discriminator == "NoUsefulProposal")
                return new(state.SubmittedProposals == 0 && state.AdmittedTrials == 0 && state.Evaluations == 0
                    ? LearningAdmissionReason.NoUsefulProposal : LearningAdmissionReason.BudgetExceeded);
            if (Text(root, "targetId", 80) != scope.Input.TargetId) return new(LearningAdmissionReason.UnsupportedScope);
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
            if (Text(criterion, "metric", 80) != Criterion.Metric || Text(criterion, "phase", 80) != Criterion.Phase ||
                Text(criterion, "comparison", 80) != nameof(LearningComparison.LessThanOrEqual) ||
                criterion.GetProperty("threshold").GetDecimal() != 0m || Text(criterion, "unit", 80) != Criterion.Unit ||
                Text(criterion, "samplePolicy", 80) != Criterion.SamplePolicy)
                return new(LearningAdmissionReason.InvalidFalsification);
            // A duplicate links an already committed identity; it never admits a new execution or exposure.
            if (state.PreviousFingerprint == scope.ExecutionFingerprint && state.PreviousProposalId is not null)
                return new(LearningAdmissionReason.Duplicate, ReusedProposalId: state.PreviousProposalId);
            if (state.Exposure != LearningExposure.Unexposed)
                return new(state.Exposure == LearningExposure.Consumed ? LearningAdmissionReason.ExposureConsumed : LearningAdmissionReason.ExposureUnknown);
            if (state.SubmittedProposals != 0 || state.AdmittedTrials != 0 || state.Evaluations != 0 ||
                state.PreviousFingerprint is not null || state.PreviousProposalId is not null)
                return new(LearningAdmissionReason.BudgetExceeded);
            var draft = new LearningProposalDraft(question, rationale,
                new(new("momentum", "v1"), 20m, SyntheticLearningScope.Handle), Criterion);
            return new(LearningAdmissionReason.Admitted, new(draft, scope, LearningIdentity.HashBytes(Encoding.UTF8.GetBytes(response))));
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
