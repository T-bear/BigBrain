using System.Globalization;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BigBrain.Modules.Finance;
using Microsoft.Data.Sqlite;

namespace BigBrain.Api.Finance;

public sealed record FinanceDatasetOptions
{
    public const string Section = "Finance:Datasets";
    public string QuarantineDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "bigbrain-finance", "dataset-quarantine");
    public string OwnerDropDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "bigbrain-finance", "market-data-drop");
    public long MaximumDownloadBytes { get; set; } = 500_000_000;
    public long MinimumFreeBytesAfterDownload { get; set; } = 2_000_000_000;
    public int MaximumArchiveFiles { get; set; } = 100;
    public long MaximumExtractedBytes { get; set; } = 1_000_000_000;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaximumRetries { get; set; } = 2;
    public int OwnerDropScanSeconds { get; set; } = 30;
    public int MaximumSidecarBytes { get; set; } = 65_536;
}

public sealed record DatasetCatalogItem(string CandidateId, string OriginalFilename, string Source, string SourceUrl, string HostingPlatform,
    string Status, string LicenseClass, string ProvenanceResult, string ArtifactSha256, long ArtifactBytes,
    string? CoverageFrom, string? CoverageTo, long ObservationCount, int InstrumentCount, string PriceBasis,
    string SurvivorshipBias, string ValidationResult, string PromotionDecision, string? CanonicalRevisionId,
    long PromotedObservationCount,IReadOnlyList<string> PromotedSymbols, IReadOnlyList<string> Limitations,
    bool CleanupEligible, string CleanupState, bool ManifestRetained, string SchemaFingerprint,
    long DuplicateKeys, long ConflictingKeys, long InvalidOhlcv, string CrossSourceComparison,
    int SafelyMappedInstruments, int UnmappedInstruments, string DeclaredLicense, string LicenseEvidenceUrl,
    string Provenance, string TechnicalQuality, string RightsStatus, string PromotionEligibility,
    string Encoding, string Delimiter, IReadOnlyList<string> Headers, string OwnerRightsDecision,
    string OwnerRightsEvidence, string ExternalRightsVerification, string OwnerDeclaredPriceBasis,
    long ZeroVolume, long OutOfOrderRows, long MissingSessions, long SuspiciousDiscontinuities,
    long SplitLikeJumps, long MissingValues, long InvalidDates, long NonPositivePrices,
    long InconsistentOhlc, long InvalidVolume);
public sealed record FinanceDatasetCatalog(DateTimeOffset GeneratedAtUtc, string OperatingMode, IReadOnlyList<DatasetCatalogItem> Datasets);
internal sealed record QuarantineCleanupResult(int EligibleCandidates, int PayloadsDeleted, long BytesReleased,
    int ManifestsRetained, int CanonicalRevisionsProtected, bool Idempotent);

internal sealed partial class FinanceDatasetIntakeStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly EodhdFinanceOptions _market;
    private readonly FinanceDatasetOptions _options;
    public FinanceDatasetIntakeStore(EodhdFinanceOptions market, FinanceDatasetOptions options)
    {
        _market=market;_options=options;Directory.CreateDirectory(options.QuarantineDirectory);Initialize();
    }
    private string ConnectionString=>new SqliteConnectionStringBuilder{DataSource=_market.DatabasePath}.ToString();
    private void Initialize(){using var c=new SqliteConnection(ConnectionString);c.Open();using var x=c.CreateCommand();x.CommandText="""
      CREATE TABLE IF NOT EXISTS dataset_candidates(candidate_id TEXT PRIMARY KEY,source TEXT NOT NULL,source_url TEXT NOT NULL,hosting_platform TEXT NOT NULL,
        filename TEXT NOT NULL,state TEXT NOT NULL,downloaded_utc TEXT,artifact_bytes INTEGER NOT NULL DEFAULT 0,artifact_sha256 TEXT NOT NULL DEFAULT '',mime_type TEXT NOT NULL DEFAULT '',
        compression TEXT NOT NULL DEFAULT '',license_class TEXT NOT NULL,declared_license TEXT NOT NULL,license_evidence_url TEXT NOT NULL,license_retrieved_on TEXT NOT NULL,
        provenance TEXT NOT NULL,provenance_result TEXT NOT NULL,downloader_version TEXT NOT NULL,acquisition_method TEXT NOT NULL,external_requests INTEGER NOT NULL DEFAULT 0,
        manifest_json TEXT,validation_json TEXT,promotion_policy TEXT,promotion_result TEXT,canonical_revision_id TEXT,updated_utc TEXT NOT NULL);
      CREATE TABLE IF NOT EXISTS dataset_candidate_files(candidate_id TEXT NOT NULL,path TEXT NOT NULL,size_bytes INTEGER NOT NULL,sha256 TEXT NOT NULL,PRIMARY KEY(candidate_id,path));
      CREATE TABLE IF NOT EXISTS dataset_corporate_actions(candidate_id TEXT NOT NULL,symbol TEXT NOT NULL,session_date TEXT NOT NULL,ex_dividend TEXT,split_ratio TEXT,
        source_row INTEGER NOT NULL,PRIMARY KEY(candidate_id,symbol,session_date));
      CREATE INDEX IF NOT EXISTS ix_dataset_state ON dataset_candidates(state,updated_utc);
      UPDATE dataset_candidates SET state=CASE WHEN state='Downloading' THEN 'Rejected' ELSE 'Downloaded' END,promotion_result='interruptedBeforeValidation',updated_utc=datetime('now') WHERE state IN ('Downloading','Inspecting','Validating');
      """;x.ExecuteNonQuery();InitializeResearchDatasetStorage(c);InitializeResearchCampaignStorage(c);if(!ColumnExists(c,"dataset_candidates","cleanup_state"))Exec(c,"ALTER TABLE dataset_candidates ADD COLUMN cleanup_state TEXT NOT NULL DEFAULT 'Retained'");
      var pending=new List<(string Id,string File)>();using(var q=c.CreateCommand()){q.CommandText="SELECT candidate_id,filename FROM dataset_candidates WHERE cleanup_state='CleanupPending'";using var r=q.ExecuteReader();while(r.Read())pending.Add((r.GetString(0),r.GetString(1)));}
      foreach(var item in pending){var exists=File.Exists(Path.Combine(_options.QuarantineDirectory,item.Id,"artifact",SafeName(item.File)));Exec(c,"UPDATE dataset_candidates SET cleanup_state=$state WHERE candidate_id=$id AND cleanup_state='CleanupPending'",("$state",exists?"Retained":"PayloadDeleted"),("$id",item.Id));}}

    internal void Discover(ExternalDatasetCandidate candidate,string method)
    {using var c=new SqliteConnection(ConnectionString);c.Open();Exec(c,"INSERT OR IGNORE INTO dataset_candidates(candidate_id,source,source_url,hosting_platform,filename,state,license_class,declared_license,license_evidence_url,license_retrieved_on,provenance,provenance_result,downloader_version,acquisition_method,updated_utc) " +
      "VALUES($id,$source,$url,$host,$file,'Discovered',$license,$declared,$evidence,$date,$provenance,$provenanceResult,'bigbrain-dataset-downloader-v1',$method,$now)",
      ("$id",candidate.CandidateId),("$source",candidate.SourceName),("$url",candidate.SourceUrl),("$host",candidate.HostingPlatform),("$file",candidate.OriginalFilename),
      ("$license",candidate.Rights.LicenseClass.ToString()),("$declared",candidate.Rights.DeclaredLicense),("$evidence",candidate.Rights.EvidenceUrl),("$date",candidate.Rights.RetrievedOn.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),
      ("$provenance",candidate.Provenance),("$provenanceResult",candidate.Rights.UnderlyingProvenance.ToString()),("$method",method),("$now",DateTimeOffset.UtcNow.ToString("O")));}

    internal async Task<string> DownloadAsync(ExternalDatasetCandidate candidate,CancellationToken token)
    {
        Discover(candidate,"https");Transition(candidate.CandidateId,DatasetCandidateState.Discovered,DatasetCandidateState.Downloading);
        var expected=candidate.ExpectedBytes??_options.MaximumDownloadBytes;EnsureDisk(expected);
        var target=Path.Combine(_options.QuarantineDirectory,candidate.CandidateId,"artifact",SafeName(candidate.OriginalFilename));Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temp=target+".partial";var requests=0;
        using var client=new HttpClient(new SocketsHttpHandler{AllowAutoRedirect=true}){Timeout=TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds,10,300))};
        try{for(var attempt=0;;attempt++){requests++;try{using var response=await client.GetAsync(candidate.SourceUrl,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
          var length=response.Content.Headers.ContentLength;if(length>_options.MaximumDownloadBytes)throw new InvalidDataException("Artifact exceeds configured size limit.");
          await using var input=await response.Content.ReadAsStreamAsync(token);await using var output=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,81920,FileOptions.Asynchronous);
          var buffer=new byte[81920];long total=0;for(int read;(read=await input.ReadAsync(buffer,token))>0;){total+=read;if(total>_options.MaximumDownloadBytes)throw new InvalidDataException("Artifact exceeds configured size limit.");await output.WriteAsync(buffer.AsMemory(0,read),token);}break;
        }catch(HttpRequestException) when(attempt<Math.Clamp(_options.MaximumRetries,0,3)){await Task.Delay(250*(1<<attempt),token);}}
        File.Move(temp,target,true);RecordArtifact(candidate.CandidateId,target,requests);Transition(candidate.CandidateId,DatasetCandidateState.Downloading,DatasetCandidateState.Downloaded);return target;}
        catch{RecordRequests(candidate.CandidateId,requests);RejectIncomplete(candidate.CandidateId,"downloadFailed");throw;}
    }

    internal DatasetCatalogItem InspectValidatePromote(ExternalDatasetCandidate candidate,string artifactPath) =>
        InspectValidate(candidate, artifactPath, true);

    internal DatasetCatalogItem InspectValidateForReview(ExternalDatasetCandidate candidate,string artifactPath) =>
        InspectValidate(candidate, artifactPath, false);

    private DatasetCatalogItem InspectValidate(ExternalDatasetCandidate candidate,string artifactPath,bool promoteWhenApproved)
    {
        Discover(candidate,"local-or-supported-external-acquisition");EnsureArtifactRecorded(candidate.CandidateId,artifactPath);
        var state=State(candidate.CandidateId);if(state is DatasetCandidateState.Approved or DatasetCandidateState.Promoted or DatasetCandidateState.ManualReviewRequired or DatasetCandidateState.Rejected)return Catalog().Datasets.Single(x=>x.CandidateId==candidate.CandidateId);if(state==DatasetCandidateState.Discovered){Transition(candidate.CandidateId,state,DatasetCandidateState.Downloading);Transition(candidate.CandidateId,DatasetCandidateState.Downloading,DatasetCandidateState.Downloaded);}
        Transition(candidate.CandidateId,DatasetCandidateState.Downloaded,DatasetCandidateState.Inspecting);
        var csv=PrepareCsv(candidate.CandidateId,artifactPath);Transition(candidate.CandidateId,DatasetCandidateState.Inspecting,DatasetCandidateState.Validating);
        var parsed=ParseCsv(csv,candidate);var gates=BuildGates(candidate,parsed);var validation=new DatasetValidationSummary(gates,DatasetContentIdentity.SchemaFingerprint(parsed.Headers),parsed.Rows.Count,
          parsed.Rows.Select(x=>x.Symbol).Distinct(StringComparer.Ordinal).Count(),parsed.Rows.Count==0?null:parsed.Rows.Min(x=>x.Date),parsed.Rows.Count==0?null:parsed.Rows.Max(x=>x.Date),parsed.Duplicates,parsed.Conflicts,parsed.InvalidOhlcv,
          parsed.Overlap.Classification,parsed.Limitations.ToImmutableArray(),parsed.ZeroVolume,parsed.OutOfOrderRows,
          parsed.MissingSessions,parsed.SuspiciousDiscontinuities,parsed.SplitLikeJumps,parsed.MissingValues,
          parsed.InvalidDates,parsed.NonPositivePrices,parsed.InconsistentOhlc,parsed.InvalidVolume);var decision=DatasetPromotionPolicyV1.Decide(validation);string? revision=null;
        if(decision.Result==DatasetEvidenceResult.Pass){Transition(candidate.CandidateId,DatasetCandidateState.Validating,DatasetCandidateState.Approved);if(promoteWhenApproved){revision=Promote(candidate,parsed);Transition(candidate.CandidateId,DatasetCandidateState.Approved,DatasetCandidateState.Promoted);}else{parsed.Limitations.Add("ExplicitPromotionReviewRequired");validation=validation with{Limitations=parsed.Limitations.ToImmutableArray()};}}
        else Transition(candidate.CandidateId,DatasetCandidateState.Validating,decision.State);
        PersistDecision(candidate,parsed,validation,decision,revision);return Catalog().Datasets.Single(x=>x.CandidateId==candidate.CandidateId);
    }

    internal DatasetCatalogItem RejectQuarantinedInspection(ExternalDatasetCandidate candidate,string artifactPath,string reason)
    {
        Discover(candidate,"owner-drop");EnsureArtifactRecorded(candidate.CandidateId,artifactPath);
        RejectIncomplete(candidate.CandidateId,reason);
        return Catalog().Datasets.Single(x=>x.CandidateId==candidate.CandidateId);
    }

    internal FinanceDatasetCatalog Catalog(){using var c=new SqliteConnection(ConnectionString);c.Open();using var x=c.CreateCommand();x.CommandText="SELECT candidate_id,filename,source,source_url,hosting_platform,state,license_class,provenance_result,artifact_sha256,artifact_bytes,validation_json,promotion_result,canonical_revision_id,(SELECT COUNT(*) FROM observations o WHERE o.revision_id=dataset_candidates.canonical_revision_id),cleanup_state,manifest_json,declared_license,license_evidence_url,provenance FROM dataset_candidates ORDER BY updated_utc DESC,candidate_id";using var r=x.ExecuteReader();var rows=new List<DatasetCatalogItem>();while(r.Read()){var v=r.IsDBNull(10)?null:JsonSerializer.Deserialize<StoredValidation>(r.GetString(10),Json);var revision=r.IsDBNull(12)?null:r.GetString(12);var state=r.GetString(5);var cleanup=r.GetString(14);var license=r.GetString(6);var provenanceResult=r.GetString(7);var quality=v is null?"FAIL":v.TechnicalQuality;var rights=license==DatasetLicenseClass.Incompatible.ToString()?"REJECTED":license==DatasetLicenseClass.Unknown.ToString()||provenanceResult!=DatasetEvidenceResult.Pass.ToString()?"HUMAN_CONFIRMATION_REQUIRED":"VERIFIED";var eligibility=revision is not null?"PROMOTED":state==DatasetCandidateState.Approved.ToString()?"READY_FOR_EXPLICIT_PROMOTION_REVIEW":"BLOCKED";rows.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),state,license,provenanceResult,r.GetString(8),r.GetInt64(9),v?.From,v?.To,v?.Rows??0,v?.Symbols??0,v?.PriceBasis??"Unclear",v?.Survivorship??"SurvivorshipUnknown",v?.Result??"Unknown",r.IsDBNull(11)?"pending":r.GetString(11),revision,r.GetInt64(13),revision is null?[]:v?.PromotedSymbols??[],v?.Limitations??[],state=="Rejected"&&cleanup=="Retained",cleanup,!r.IsDBNull(15),v?.SchemaFingerprint??"",v?.DuplicateKeys??0,v?.ConflictingKeys??0,v?.InvalidOhlcv??0,v?.Comparison??DatasetComparisonClass.InsufficientOverlap.ToString(),v?.SafelyMappedInstruments??0,v?.UnmappedInstruments??0,r.GetString(16),r.GetString(17),r.GetString(18),quality,rights,eligibility,v?.Encoding??"UNKNOWN",v?.Delimiter??"UNKNOWN",v?.Headers??[],v?.OwnerRightsDecision??DatasetOwnerRightsDecision.NotProvided.ToString(),v?.OwnerRightsEvidence??"",v?.ExternalRightsVerification??DatasetEvidenceResult.Unknown.ToString(),v?.OwnerDeclaredPriceBasis??"UNKNOWN",v?.ZeroVolume??0,v?.OutOfOrderRows??0,v?.MissingSessions??0,v?.SuspiciousDiscontinuities??0,v?.SplitLikeJumps??0,v?.MissingValues??0,v?.InvalidDates??0,v?.NonPositivePrices??0,v?.InconsistentOhlc??0,v?.InvalidVolume??0));}return new(DateTimeOffset.UtcNow,"RESEARCH",rows);}

    internal QuarantineCleanupResult CleanupRejected(DateTimeOffset cutoffUtc)
    {
        using var c=new SqliteConnection(ConnectionString);c.Open();var candidates=new List<(string Id,string File,long Bytes)>();
        using(var x=c.CreateCommand()){x.CommandText="SELECT d.candidate_id,d.filename,d.artifact_bytes FROM dataset_candidates d WHERE d.state='Rejected' AND d.cleanup_state='Retained' AND d.updated_utc<=$cutoff ORDER BY d.candidate_id";x.Parameters.AddWithValue("$cutoff",cutoffUtc.ToString("O"));using var r=x.ExecuteReader();while(r.Read())candidates.Add((r.GetString(0),r.GetString(1),r.GetInt64(2)));}
        var canonical=Convert.ToInt32(Scalar(c,"SELECT CAST(COUNT(*) AS TEXT) FROM dataset_candidates WHERE state='Promoted' AND canonical_revision_id IS NOT NULL")??"0",CultureInfo.InvariantCulture);var deleted=0;long released=0;
        foreach(var item in candidates){Exec(c,"UPDATE dataset_candidates SET cleanup_state='CleanupPending' WHERE candidate_id=$id AND state='Rejected' AND cleanup_state='Retained'",("$id",item.Id));var path=Path.Combine(_options.QuarantineDirectory,item.Id,"artifact",SafeName(item.File));if(File.Exists(path)){File.Delete(path);deleted++;released+=item.Bytes;}Exec(c,"UPDATE dataset_candidates SET cleanup_state='PayloadDeleted' WHERE candidate_id=$id AND state='Rejected' AND cleanup_state='CleanupPending'",("$id",item.Id));}
        return new(candidates.Count,deleted,released,candidates.Count,canonical,candidates.Count==0);
    }

    private ParsedDataset ParseCsv(string path,ExternalDatasetCandidate candidate)
    {var rows=new List<ImportBar>();var seen=new Dictionary<string,ImportBar>(StringComparer.Ordinal);var lastSourceDate=new Dictionary<string,DateOnly>(StringComparer.Ordinal);long duplicates=0,conflicts=0,invalid=0,zeroVolume=0,outOfOrder=0,missingValues=0,invalidDates=0,nonPositive=0,inconsistent=0,invalidVolume=0;using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);using var reader=new StreamReader(stream,new UTF8Encoding(false,true),true,65536);
      var headerLine=reader.ReadLine()??throw new InvalidDataException("CSV is empty.");if(headerLine.Length>64_000)throw new InvalidDataException("CSV header is too large.");var headers=Csv(headerLine).Select(x=>x.Trim()).ToArray();var map=headers.Select((h,i)=>(h:Normalize(h),i)).ToDictionary(x=>x.h,x=>x.i,StringComparer.Ordinal);
      var ticker=Column(map,"ticker","symbol");var date=Column(map,"date");var open=Column(map,"open");var high=Column(map,"high");var low=Column(map,"low");var close=Column(map,"close");var volume=Column(map,"volume");var hasAdjusted=map.TryGetValue("adjclose",out var adjusted);map.TryGetValue("exdividend",out var ex);map.TryGetValue("splitratio",out var split);
      string? line;long ordinal=1;while((line=reader.ReadLine()) is not null){ordinal++;if(line.Length>1_000_000)throw new InvalidDataException("CSV line exceeds safety limit.");var f=Csv(line);if(f.Count!=headers.Length)throw new InvalidDataException($"Malformed CSV field count at row {ordinal}.");
        if(new[]{ticker,date,open,high,low,close,volume}.Any(i=>string.IsNullOrWhiteSpace(f[i]))){missingValues++;invalid++;continue;}
        if(!DateOnly.TryParseExact(f[date],"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var d)){invalidDates++;invalid++;continue;}
        if(!Dec(f[open],out var o)||!Dec(f[high],out var h)||!Dec(f[low],out var l)||!Dec(f[close],out var cl)||!Dec(f[volume],out var vol)){invalid++;continue;}
        decimal? adj=null;if(hasAdjusted&&!string.IsNullOrWhiteSpace(f[adjusted])){if(!Dec(f[adjusted],out var parsedAdjusted)||parsedAdjusted<=0){invalid++;continue;}adj=parsedAdjusted;}
        var symbol=f[ticker].Trim().ToUpperInvariant();if(symbol.Length is 0 or >32||symbol[0] is '=' or '+' or '-' or '@'){invalid++;continue;}var row=new ImportBar(symbol,d,o,h,l,cl,adj,vol,ex>0?f[ex]:null,split>0?f[split]:null,ordinal);
        if(o<=0||h<=0||l<=0||cl<=0){nonPositive++;invalid++;continue;}if(vol<0){invalidVolume++;invalid++;continue;}if(h<Math.Max(o,cl)||l>Math.Min(o,cl)||l>h){inconsistent++;invalid++;continue;}if(vol==0)zeroVolume++;if(lastSourceDate.TryGetValue(symbol,out var previous)&&d<previous)outOfOrder++;lastSourceDate[symbol]=d;var key=$"{symbol}|{d:yyyy-MM-dd}";if(seen.TryGetValue(key,out var old)){if(old==row with{Row=old.Row})duplicates++;else conflicts++;continue;}seen.Add(key,row);rows.Add(row);}
      var missingSessions=0L;var discontinuities=0L;var splitLike=0L;foreach(var symbolRows in rows.GroupBy(x=>x.Symbol,StringComparer.Ordinal)){var ordered=symbolRows.OrderBy(x=>x.Date).ToArray();if(ordered.Length==0)continue;var dates=ordered.Select(x=>x.Date).ToHashSet();for(var day=ordered[0].Date;day<=ordered[^1].Date;day=day.AddDays(1))if(UsMarketCalendar.IsSession(day)&&!dates.Contains(day))missingSessions++;for(var i=1;i<ordered.Length;i++){var ratio=ordered[i].Close/ordered[i-1].Close;var change=Math.Abs(ratio-1m);if(change>=0.20m)discontinuities++;if(ratio<=0.55m||ratio>=1.80m)splitLike++;}}
      var overlap=CompareExisting(rows,candidate.PriceBasis);var limitations=new List<string>();if(candidate.SurvivorshipBias!=DatasetSurvivorshipBias.PointInTimeUniverse)limitations.Add(candidate.SurvivorshipBias.ToString());if(candidate.PriceBasis==DatasetPriceBasis.Unclear)limitations.Add("PriceBasisUnclear");if(invalid>0)limitations.Add($"RejectedInvalidRows:{invalid}");if(zeroVolume>0)limitations.Add($"ZeroVolumeRows:{zeroVolume}");if(outOfOrder>0)limitations.Add($"OutOfOrderRows:{outOfOrder}");if(missingSessions>0)limitations.Add($"MissingCalendarSessions:{missingSessions}");if(discontinuities>0)limitations.Add($"SuspiciousCloseDiscontinuities:{discontinuities}");if(splitLike>0)limitations.Add($"SplitLikeJumpsWithoutCorporateActionEvidence:{splitLike}");if(candidate.OwnerRightsDecision==DatasetOwnerRightsDecision.ApprovedByOwner)limitations.Add("ExternalRightsVerificationUnknownDespiteOwnerApproval");return new(headers,rows,duplicates,conflicts,invalid,overlap,limitations,zeroVolume,outOfOrder,missingSessions,discontinuities,splitLike,missingValues,invalidDates,nonPositive,inconsistent,invalidVolume);}

    private DatasetOverlapMetrics CompareExisting(List<ImportBar> rows,DatasetPriceBasis basis){using var c=new SqliteConnection(ConnectionString);c.Open();using var x=c.CreateCommand();x.CommandText="SELECT symbol,session_date,open,high,low,close,volume FROM (SELECT symbol,session_date,open,high,low,close,volume,ROW_NUMBER() OVER(PARTITION BY instrument_id,session_date ORDER BY acquired_utc DESC,revision_id DESC) rank FROM observations WHERE provider='EODHD') WHERE rank=1";using var r=x.ExecuteReader();var existing=new List<DatasetComparableBar>();while(r.Read())existing.Add(new(r.GetString(0),DateOnly.Parse(r.GetString(1),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(2),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(3),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(4),CultureInfo.InvariantCulture),decimal.Parse(r.GetString(5),CultureInfo.InvariantCulture),r.GetInt64(6),DatasetPriceBasis.Raw));return DatasetCrossSourceComparerV1.Compare(rows.Select(x=>new DatasetComparableBar(x.Symbol,x.Date,x.Open,x.High,x.Low,x.Close,x.Volume,basis)),existing);}

    private static System.Collections.Immutable.ImmutableArray<DatasetGateResult> BuildGates(ExternalDatasetCandidate c,ParsedDataset p)
    {DatasetGateResult G(DatasetGate gate,DatasetEvidenceResult result,string code)=>new(gate,result,code,code);var overlap=p.Overlap.Classification switch{DatasetComparisonClass.MaterialConflict=>DatasetEvidenceResult.Fail,DatasetComparisonClass.InsufficientOverlap=>DatasetEvidenceResult.Pass,_=>DatasetEvidenceResult.Pass};return
      [G(DatasetGate.Integrity,p.Rows.Count>0?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Fail,"artifact.sha256.and.nonempty"),
       G(DatasetGate.License,c.Rights.LicenseClass==DatasetLicenseClass.Unknown?DatasetEvidenceResult.Unknown:c.Rights.LicenseClass is DatasetLicenseClass.PublicDomain or DatasetLicenseClass.Cc0 or DatasetLicenseClass.CcBy or DatasetLicenseClass.CompatibleOther?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Fail,"rights.explicit.compatible"),
       G(DatasetGate.Provenance,c.Rights.UnderlyingProvenance,"provenance.underlying.source"),G(DatasetGate.Schema,p.Headers.Length>=7?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Fail,"schema.required.fields"),
       G(DatasetGate.FieldSemantics,c.PriceBasis==DatasetPriceBasis.Unclear?DatasetEvidenceResult.Unknown:DatasetEvidenceResult.Pass,"semantics.price.basis"),G(DatasetGate.DateTime,p.Rows.Count>0?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Fail,"date.iso.daily"),
       G(DatasetGate.Ohlcv,p.Rows.Count>0&&p.InvalidOhlcv*100m/(p.Rows.Count+p.InvalidOhlcv)<=1m?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Fail,"ohlcv.invalid.row.rate.max-1-percent"),G(DatasetGate.DuplicateConflict,p.Conflicts==0?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Fail,"duplicates.no.conflict"),
       G(DatasetGate.SymbolMapping,p.Rows.Any(x=>Watchlist.ContainsKey(x.Symbol))?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Fail,"symbols.effective.mapping"),G(DatasetGate.SurvivorshipCoverage,DatasetEvidenceResult.Pass,"survivorship.explicit"),
       G(DatasetGate.CorporateActions,c.PriceBasis==DatasetPriceBasis.RawAndAdjusted?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Unknown,"corporate.actions.explicit"),G(DatasetGate.SourceOverlap,overlap,"overlap.versioned.thresholds"),
       G(DatasetGate.EntitlementRetention,c.Rights.LocalRetentionAllowed?DatasetEvidenceResult.Pass:c.Rights.LicenseClass==DatasetLicenseClass.Unknown?DatasetEvidenceResult.Unknown:DatasetEvidenceResult.Fail,"retention.local.allowed")];}

    private string Promote(ExternalDatasetCandidate candidate,ParsedDataset parsed){var selected=parsed.Rows.Where(x=>Watchlist.ContainsKey(x.Symbol)).OrderBy(x=>x.Symbol,StringComparer.Ordinal).ThenBy(x=>x.Date).ToArray();if(selected.Length==0)throw new InvalidOperationException("No safely mapped watchlist rows are available.");var content=string.Join("\n",selected.Select(x=>$"{x.Symbol}|{x.Date:yyyy-MM-dd}|{x.Open}|{x.High}|{x.Low}|{x.Close}|{x.AdjustedClose}|{x.Volume}"));var hash="sha256:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();var prefix=candidate.SourceName=="NASDAQ-WIKI"?"wiki":Normalize(candidate.SourceName);var revision=$"{prefix}-{hash[7..23]}";var product=candidate.SourceName=="NASDAQ-WIKI"?"WIKI/PRICES":candidate.CandidateId;using var c=new SqliteConnection(ConnectionString);c.Open();using var t=c.BeginTransaction();var now=DateTimeOffset.UtcNow.ToString("O");
      foreach(var row in selected){var i=Watchlist[row.Symbol];Exec(c,t,"INSERT OR IGNORE INTO observations VALUES($provider,$product,$policy,$instrument,$symbol,$providerSymbol,$mic,$date,$open,$high,$low,$close,$adjusted,$volume,$acquired,$revision)",("$provider",candidate.SourceName),("$product",product),("$policy",DatasetPromotionPolicyV1.Id),("$instrument",i.InstrumentId),("$symbol",row.Symbol),("$providerSymbol",row.Symbol),("$mic",i.Mic),("$date",row.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$open",Text(row.Open)),("$high",Text(row.High)),("$low",Text(row.Low)),("$close",Text(row.Close)),("$adjusted",row.AdjustedClose is null?"":Text(row.AdjustedClose.Value)),("$volume",Convert.ToInt64(row.Volume)),("$acquired",now),("$revision",revision));if(row.ExDividend is not null||row.SplitRatio is not null)Exec(c,t,"INSERT OR IGNORE INTO dataset_corporate_actions VALUES($candidate,$symbol,$date,$dividend,$split,$row)",("$candidate",candidate.CandidateId),("$symbol",row.Symbol),("$date",row.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$dividend",row.ExDividend??""),("$split",row.SplitRatio??""),("$row",row.Row));}
      Exec(c,t,"INSERT OR IGNORE INTO revisions VALUES($id,$hash,$created,$count,1)",( "$id",revision),("$hash",hash),("$created",now),("$count",selected.Length));var adjusted=selected.All(x=>x.AdjustedClose is not null)&&candidate.PriceBasis==DatasetPriceBasis.RawAndAdjusted?AdjustedPriceCapability.RawAndAdjustedValid:selected.All(x=>x.AdjustedClose is null)?AdjustedPriceCapability.AdjustedUnavailable:AdjustedPriceCapability.AuditRequired;Exec(c,t,"INSERT OR IGNORE INTO revision_price_capabilities VALUES($id,'RAW_ONLY_VALID',$adjusted,'dataset-promotion-v2',$evidence,$at)",( "$id",revision),("$adjusted",adjusted.ToString()),("$evidence",adjusted==AdjustedPriceCapability.RawAndAdjustedValid?"Validated source provided distinct raw and adjusted fields.":"Adjusted capability is unavailable or requires explicit audit."),("$at",now));t.Commit();return revision;}

    private string PrepareCsv(string id,string artifact)
    {
        if(!File.Exists(artifact))throw new FileNotFoundException("Quarantine artifact is unavailable.",artifact);
        if(Path.GetExtension(artifact).Equals(".csv",StringComparison.OrdinalIgnoreCase)||LooksLikeCsv(artifact))return artifact;
        if(!Path.GetExtension(artifact).Equals(".zip",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Only CSV and ZIP containing CSV are supported.");
        var root=Path.Combine(_options.QuarantineDirectory,id,"extracted");Directory.CreateDirectory(root);using var zip=ZipFile.OpenRead(artifact);
        if(zip.Entries.Count>_options.MaximumArchiveFiles)throw new InvalidDataException("Archive contains too many files.");long total=0;var csvFiles=new List<string>();
        foreach(var e in zip.Entries){if(string.IsNullOrEmpty(e.Name))continue;if(e.FullName.StartsWith('/')||e.FullName.StartsWith('\\')||Path.IsPathRooted(e.FullName)||e.FullName.Split('/','\\').Any(x=>x==".."))throw new InvalidDataException("Unsafe archive path.");total+=e.Length;if(total>_options.MaximumExtractedBytes)throw new InvalidDataException("Archive exceeds extraction limit.");if(!Path.GetExtension(e.Name).Equals(".csv",StringComparison.OrdinalIgnoreCase)||e.Name.Equals("manifest.csv",StringComparison.OrdinalIgnoreCase))continue;var target=Path.GetFullPath(Path.Combine(root,e.FullName));if(!target.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Archive path escapes quarantine.");Directory.CreateDirectory(Path.GetDirectoryName(target)!);e.ExtractToFile(target,true);csvFiles.Add(target);}
        if(csvFiles.Count==0)throw new InvalidDataException("Archive contains no market CSV.");if(csvFiles.Count==1)return csvFiles[0];
        var combined=Path.Combine(root,"combined-market-data.csv");using var output=new StreamWriter(combined,false,new UTF8Encoding(false));output.WriteLine("ticker,date,open,high,low,close,volume,adj_close");
        foreach(var file in csvFiles.Order(StringComparer.Ordinal)){using var input=new StreamReader(file,new UTF8Encoding(false,true),true,65536);var header=Csv(input.ReadLine()??throw new InvalidDataException("Archive CSV is empty.")).Select(Normalize).ToArray();var date=Array.IndexOf(header,"date");var open=Array.IndexOf(header,"open");var high=Array.IndexOf(header,"high");var low=Array.IndexOf(header,"low");var close=Array.IndexOf(header,"close");var volume=Array.IndexOf(header,"volume");var adjusted=Array.IndexOf(header,"adjclose");if(new[]{date,open,high,low,close,volume}.Any(x=>x<0))throw new InvalidDataException("Archive CSV lacks OHLCV fields.");string? line;while((line=input.ReadLine()) is not null){if(line.Length>1_000_000)throw new InvalidDataException("CSV line exceeds safety limit.");var f=Csv(line);output.WriteLine(string.Join(',',Path.GetFileNameWithoutExtension(file),f[date],f[open],f[high],f[low],f[close],f[volume],adjusted>=0?f[adjusted]:""));}}
        return combined;
    }
    private static bool LooksLikeCsv(string path){using var stream=File.OpenRead(path);var bytes=new byte[Math.Min(4096,(int)Math.Min(stream.Length,4096))];var read=stream.Read(bytes);if(read==0||bytes.AsSpan(0,read).Contains((byte)0))return false;var first=Encoding.UTF8.GetString(bytes,0,read).Split('\n')[0];var fields=Csv(first).Select(Normalize).ToHashSet(StringComparer.Ordinal);return fields.Contains("date")&&fields.Contains("open")&&fields.Contains("high")&&fields.Contains("low")&&fields.Contains("close");}
    private void RecordArtifact(string id,string path,int requests){using var s=File.OpenRead(path);var hash=DatasetContentIdentity.Sha256(s);var info=new FileInfo(path);using var c=new SqliteConnection(ConnectionString);c.Open();Exec(c,"UPDATE dataset_candidates SET artifact_bytes=$bytes,artifact_sha256=$hash,mime_type=$mime,compression=$compression,external_requests=external_requests+$requests,downloaded_utc=$now,updated_utc=$now WHERE candidate_id=$id",("$bytes",info.Length),("$hash",hash),("$mime",Path.GetExtension(path).Equals(".zip",StringComparison.OrdinalIgnoreCase)?"application/zip":"text/csv"),("$compression",Path.GetExtension(path).TrimStart('.').ToLowerInvariant()),("$requests",requests),("$now",DateTimeOffset.UtcNow.ToString("O")),("$id",id));Exec(c,"INSERT OR REPLACE INTO dataset_candidate_files VALUES($id,$path,$bytes,$hash)",( "$id",id),("$path",Path.GetFileName(path)),("$bytes",info.Length),("$hash",hash));}
    private void EnsureArtifactRecorded(string id,string path){using var c=new SqliteConnection(ConnectionString);c.Open();using var x=c.CreateCommand();x.CommandText="SELECT artifact_sha256 FROM dataset_candidates WHERE candidate_id=$id";x.Parameters.AddWithValue("$id",id);var stored=x.ExecuteScalar() as string;if(string.IsNullOrEmpty(stored)){RecordArtifact(id,path,0);return;}using var stream=File.OpenRead(path);var actual=DatasetContentIdentity.Sha256(stream);if(!string.Equals(stored,actual,StringComparison.Ordinal))throw new InvalidDataException("Candidate ID is already bound to a different immutable artifact checksum; create a new candidate revision.");}
    private void PersistDecision(ExternalDatasetCandidate c,ParsedDataset p,DatasetValidationSummary v,DatasetPromotionDecision d,string? revision){var mapped=p.Rows.Where(x=>Watchlist.ContainsKey(x.Symbol)).Select(x=>x.Symbol).Distinct().Order().ToArray();var unmapped=p.Rows.Select(x=>x.Symbol).Distinct(StringComparer.Ordinal).Except(mapped,StringComparer.Ordinal).Count();var externalRights=c.Rights.LicenseClass!=DatasetLicenseClass.Unknown&&c.Rights.UnderlyingProvenance==DatasetEvidenceResult.Pass?DatasetEvidenceResult.Pass:DatasetEvidenceResult.Unknown;var technical=v.Gates.Where(x=>x.Gate is DatasetGate.Integrity or DatasetGate.Schema or DatasetGate.DateTime or DatasetGate.Ohlcv or DatasetGate.DuplicateConflict).Any(x=>x.Result==DatasetEvidenceResult.Fail)?"FAIL":v.InvalidOhlcv>0||v.DuplicateKeys>0||v.ZeroVolume>0||v.MissingSessions>0||v.SuspiciousDiscontinuities>0?"LIMITED":"PASS";var stored=new StoredValidation(v.Overall.ToString(),v.ObservationCount,v.InstrumentCount,v.CoverageFrom?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),v.CoverageTo?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),c.PriceBasis.ToString(),c.SurvivorshipBias.ToString(),mapped,v.Limitations,v.SchemaFingerprint,v.DuplicateKeys,v.ConflictingKeys,v.InvalidOhlcv,v.Comparison.ToString(),mapped.Length,unmapped,"UTF-8","comma",p.Headers,c.OwnerRightsDecision.ToString(),c.OwnerRightsEvidence,externalRights.ToString(),c.OwnerDeclaredPriceBasis,technical,v.ZeroVolume,v.OutOfOrderRows,v.MissingSessions,v.SuspiciousDiscontinuities,v.SplitLikeJumps,v.MissingValues,v.InvalidDates,v.NonPositivePrices,v.InconsistentOhlc,v.InvalidVolume);using var db=new SqliteConnection(ConnectionString);db.Open();Exec(db,"UPDATE dataset_candidates SET manifest_json=$manifest,validation_json=$validation,promotion_policy=$policy,promotion_result=$result,canonical_revision_id=$revision,updated_utc=$now WHERE candidate_id=$id",("$manifest",JsonSerializer.Serialize(new{c.CandidateId,c.SourceName,c.SourceUrl,c.Rights,c.Provenance,c.OwnerRightsDecision,c.OwnerRightsEvidence,c.OwnerDeclaredPriceBasis,ExternalRightsVerification=externalRights,Artifact=ArtifactHash(db,c.CandidateId),Files=Files(db,c.CandidateId),SchemaFingerprint=v.SchemaFingerprint,CoverageFrom=v.CoverageFrom,CoverageTo=v.CoverageTo,v.InstrumentCount,v.ObservationCount,c.PriceBasis,c.SurvivorshipBias,Validation=v,Decision=d,CanonicalRevision=revision},Json)),("$validation",JsonSerializer.Serialize(stored,Json)),("$policy",d.PolicyId),("$result",d.Result.ToString()),("$revision",(object?)revision??DBNull.Value),("$now",DateTimeOffset.UtcNow.ToString("O")),("$id",c.CandidateId));}
    private static string ArtifactHash(SqliteConnection c,string id)=>Scalar(c,"SELECT artifact_sha256 FROM dataset_candidates WHERE candidate_id=$id",("$id",id))??"";private static string[] Files(SqliteConnection c,string id){using var x=c.CreateCommand();x.CommandText="SELECT path FROM dataset_candidate_files WHERE candidate_id=$id ORDER BY path";x.Parameters.AddWithValue("$id",id);using var r=x.ExecuteReader();var a=new List<string>();while(r.Read())a.Add(r.GetString(0));return a.ToArray();}
    private void EnsureDisk(long expected){var root=Path.GetPathRoot(Path.GetFullPath(_options.QuarantineDirectory))!;var drive=new DriveInfo(root);if(expected>_options.MaximumDownloadBytes||drive.AvailableFreeSpace-expected<_options.MinimumFreeBytesAfterDownload)throw new IOException("Dataset download blocked by configured size/disk safety gate.");}
    private DatasetCandidateState State(string id){using var c=new SqliteConnection(ConnectionString);c.Open();return Enum.Parse<DatasetCandidateState>(Scalar(c,"SELECT state FROM dataset_candidates WHERE candidate_id=$id",("$id",id))!);}
    private void Transition(string id,DatasetCandidateState from,DatasetCandidateState to){DatasetCandidateStateMachine.EnsureTransition(from,to);using var c=new SqliteConnection(ConnectionString);c.Open();Exec(c,"UPDATE dataset_candidates SET state=$to,updated_utc=$now WHERE candidate_id=$id AND state=$from",("$to",to.ToString()),("$now",DateTimeOffset.UtcNow.ToString("O")),("$id",id),("$from",from.ToString()));}
    private void RejectIncomplete(string id,string reason){using var c=new SqliteConnection(ConnectionString);c.Open();Exec(c,"UPDATE dataset_candidates SET state='Rejected',promotion_result=$reason,updated_utc=$now WHERE candidate_id=$id",("$reason",reason),("$now",DateTimeOffset.UtcNow.ToString("O")),("$id",id));}
    private void RecordRequests(string id,int requests){using var c=new SqliteConnection(ConnectionString);c.Open();Exec(c,"UPDATE dataset_candidates SET external_requests=external_requests+$requests WHERE candidate_id=$id",("$requests",requests),("$id",id));}
    private static readonly Dictionary<string,EodhdInstrument> Watchlist=EodhdCatalog.Watchlist.ToDictionary(x=>x.Symbol,StringComparer.Ordinal);
    private static int Column(Dictionary<string,int> map,params string[] names){foreach(var n in names)if(map.TryGetValue(n,out var i))return i;throw new InvalidDataException($"CSV is missing required field {string.Join('/',names)}.");}
    private static string Normalize(string x)=>new(x.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());private static bool Dec(string x,out decimal value)=>decimal.TryParse(x,NumberStyles.Float,CultureInfo.InvariantCulture,out value);
    internal static List<string> Csv(string line){var fields=new List<string>();var value=new StringBuilder();var quoted=false;for(var i=0;i<line.Length;i++){var ch=line[i];if(ch=='\"'){if(quoted&&i+1<line.Length&&line[i+1]=='\"'){value.Append('\"');i++;}else quoted=!quoted;}else if(ch==','&&!quoted){fields.Add(value.ToString());value.Clear();}else value.Append(ch);}if(quoted)throw new InvalidDataException("Unterminated quoted CSV field.");fields.Add(value.ToString());return fields;}
    private static string SafeName(string x){var name=Path.GetFileName(x);if(string.IsNullOrWhiteSpace(name)||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new ArgumentException("Invalid artifact filename.");return name;}
    private static string Text(decimal x)=>x.ToString(CultureInfo.InvariantCulture);private static string? Scalar(SqliteConnection c,string sql,params(string,object)[] args){using var x=c.CreateCommand();x.CommandText=sql;foreach(var a in args)x.Parameters.AddWithValue(a.Item1,a.Item2);return x.ExecuteScalar() as string;}
    private static bool ColumnExists(SqliteConnection c,string table,string column){using var x=c.CreateCommand();x.CommandText=$"PRAGMA table_info({table})";using var r=x.ExecuteReader();while(r.Read())if(string.Equals(r.GetString(1),column,StringComparison.Ordinal))return true;return false;}
    private static void Exec(SqliteConnection c,string sql,params(string,object)[] args)=>Exec(c,null,sql,args);private static void Exec(SqliteConnection c,SqliteTransaction? t,string sql,params(string,object)[] args){using var x=c.CreateCommand();x.Transaction=t;x.CommandText=sql;foreach(var a in args)x.Parameters.AddWithValue(a.Item1,a.Item2);x.ExecuteNonQuery();}
    private sealed record ImportBar(string Symbol,DateOnly Date,decimal Open,decimal High,decimal Low,decimal Close,decimal? AdjustedClose,decimal Volume,string? ExDividend,string? SplitRatio,long Row);
    private sealed record ParsedDataset(string[] Headers,List<ImportBar> Rows,long Duplicates,long Conflicts,long InvalidOhlcv,DatasetOverlapMetrics Overlap,List<string> Limitations,long ZeroVolume,long OutOfOrderRows,long MissingSessions,long SuspiciousDiscontinuities,long SplitLikeJumps,long MissingValues,long InvalidDates,long NonPositivePrices,long InconsistentOhlc,long InvalidVolume);
    private sealed record StoredValidation(string Result,long Rows,int Symbols,string? From,string? To,string PriceBasis,string Survivorship,string[] PromotedSymbols,IReadOnlyList<string> Limitations,string SchemaFingerprint="",long DuplicateKeys=0,long ConflictingKeys=0,long InvalidOhlcv=0,string Comparison="InsufficientOverlap",int SafelyMappedInstruments=0,int UnmappedInstruments=0,string Encoding="UNKNOWN",string Delimiter="UNKNOWN",string[]? Headers=null,string OwnerRightsDecision="NotProvided",string OwnerRightsEvidence="",string ExternalRightsVerification="Unknown",string OwnerDeclaredPriceBasis="UNKNOWN",string TechnicalQuality="UNKNOWN",long ZeroVolume=0,long OutOfOrderRows=0,long MissingSessions=0,long SuspiciousDiscontinuities=0,long SplitLikeJumps=0,long MissingValues=0,long InvalidDates=0,long NonPositivePrices=0,long InconsistentOhlc=0,long InvalidVolume=0);
}

public interface IFinanceDatasetReader{FinanceDatasetCatalog GetCatalog();}
internal sealed class FinanceDatasetReader(FinanceDatasetIntakeStore store):IFinanceDatasetReader{public FinanceDatasetCatalog GetCatalog()=>store.Catalog();}

internal static class FinanceDatasetMaintenanceCommand
{
    private static readonly JsonSerializerOptions OutputJson = new(JsonSerializerDefaults.Web){WriteIndented=true};
    internal static bool TryRun(string[] args,IConfiguration configuration)
    {
        if(args.Length==0||args[0] is not ("finance-dataset-intake" or "finance-owner-drop-scan" or
            "finance-research-dataset-catalog" or "finance-research-backtest" or "finance-candidate-research-eligibility" or "finance-research-campaign"))return false;
        var market=configuration.GetSection(EodhdFinanceOptions.Section).Get<EodhdFinanceOptions>()??new();
        var options=configuration.GetSection(FinanceDatasetOptions.Section).Get<FinanceDatasetOptions>()??new();
        _ = new EodhdMarketMemory(market);
        var store=new FinanceDatasetIntakeStore(market,options);
        if(args[0]=="finance-owner-drop-scan")
        {
            if(args.Length!=1)throw new ArgumentException("Use finance-owner-drop-scan.");
            Console.WriteLine(JsonSerializer.Serialize(new FinanceOwnerDatasetDropScanner(options,store).ScanOnce(),OutputJson));return true;
        }
        if(args[0]=="finance-research-dataset-catalog")
        {
            if(args.Length!=1)throw new ArgumentException("Use finance-research-dataset-catalog.");
            Console.WriteLine(JsonSerializer.Serialize(store.ResearchCatalog(),OutputJson));return true;
        }
        if(args[0]=="finance-research-backtest")
        {
            if(args.Length!=2)throw new ArgumentException("Use finance-research-backtest <research-revision-id>.");
            Console.WriteLine(JsonSerializer.Serialize(store.RunBoundedResearchBacktest(args[1]),OutputJson));return true;
        }
        if(args[0]=="finance-research-campaign")
        {
            if(args.Length!=2||!DateTimeOffset.TryParse(args[1],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var knowledgeTime))throw new ArgumentException("Use finance-research-campaign <immutable-knowledge-time-utc>.");
            Console.WriteLine(JsonSerializer.Serialize(store.RunResearchCampaign(knowledgeTime),OutputJson));return true;
        }
        if(args[0]=="finance-candidate-research-eligibility")
        {
            if(args.Length!=2)throw new ArgumentException("Use finance-candidate-research-eligibility <candidate-id>.");
            Console.WriteLine(JsonSerializer.Serialize(store.EnsureExistingCandidateResearchEligibility(args[1]),OutputJson));return true;
        }
        if(args.Length!=3)throw new ArgumentException("Use finance-dataset-intake <wiki|zenodo> <local-artifact-path>.");
        var candidate=args[1] switch
        {
            "wiki"=>new ExternalDatasetCandidate("wiki-eod-mirror-kmfranz-v1","NASDAQ-WIKI","https://github.com/kmfranz/trading_pairs","GitHub Git LFS","WIKI_PRICES.csv",
                new(DatasetLicenseClass.PublicDomain,"Public domain","https://docs.data.nasdaq.com/v1.0/docs/in-depth-usage",new DateOnly(2026,8,15),"Nasdaq Data Link describes WIKI EOD prices, dividends and splits as released into the public domain.",DatasetEvidenceResult.Pass,true,"Nasdaq Data Link WIKI EOD"),
                "GitHub mirror of Nasdaq/Quandl WIKI/PRICES; artifact identity is validated by schema, coverage and overlap.",DatasetPriceBasis.RawAndAdjusted,DatasetSurvivorshipBias.SurvivorshipUnknown),
            "zenodo"=>new ExternalDatasetCandidate("zenodo-20192822-v1","Zenodo-20192822","https://zenodo.org/records/20192822","Zenodo","AlRidhawi_Behavioral_Evaluation_Data.zip",
                new(DatasetLicenseClass.CcBy,"CC BY 4.0","https://zenodo.org/records/20192822",new DateOnly(2026,8,15),"Authors license their curation under CC BY 4.0.",DatasetEvidenceResult.Unknown,true,"Al Ridhawi, Haj Ali and Al Osman (2026), DOI 10.5281/zenodo.20192822"),
                "Authors state observations were retrieved from Yahoo Finance through yfinance; underlying redistribution rights are not established by the record.",DatasetPriceBasis.RawAndAdjusted,DatasetSurvivorshipBias.CurrentConstituentsOnly,4_100_000),
            _=>throw new ArgumentException("Candidate must be wiki or zenodo.")
        };
        Console.WriteLine(JsonSerializer.Serialize(store.InspectValidatePromote(candidate,args[2]),OutputJson));return true;
    }
}
