# ADR 0041: Synthetic TEST operator boundary

- Status: Proposed
- Date: 2026-11-13

## Context

Trusted development candidates need a bounded synthetic Web/API slot without coupling
container lifecycle to the application. [ADR 0001](0001-web-api-must-not-control-docker.md)
is Accepted; [ADR 0002](0002-sentinel-exclusive-system-access.md) remains Proposed.
Accepted Sentinel invariants remain unchanged. The explicit
[owner decision](../operations/checkpoint-workflow.md#syntetisk-test--ägarbeslut-2026-11-13)
authorizes this U1 implementation; this Proposed ADR does not accept itself or extend product authority.

## Proposed decision

The Python operator CLI is an external development/operations tool, outside API, Web,
Brain, product Worker and AutomationEngine. None of those components can invoke it or
obtain its Docker access. It is not a Sentinel capability, adapter or privileged helper.

A separately reviewed installation snapshots the CLI and standalone Compose template,
records their hashes and refuses drift. Candidate inputs are exact locally loaded image
IDs, candidate revision and private package provenance; candidate Compose files, hooks,
inherited environments and arbitrary CLI Docker arguments are not accepted.
Two services share one internal IPv4 bridge configured with isolated gateway mode and
own labeled volumes. Parent's 2026-11-13 experiment found isolated-bridge Docker port
publication unusable and verified a fixed operator TCP-to-exec relay. U1 therefore has
no container published ports: the manually started installed CLI binds only the selected
Tailscale IPv4:13001 and forwards bytes through a fixed, exact-Web-ID `docker exec` to
Web loopback:8080. Clients cannot select a command, container or destination. Four concurrent
connections, bounded buffers, 30-second connection deadlines, 35-second in-container exec
deadlines and owned-child cleanup bound this relay. It is outside all product processes;
no extra service/container, credential, bridge address or host configuration is introduced.
The real candidate's isolation, relay behavior and Tailscale client ACLs still require
runtime evidence before a user link.

U1 is a manual rootful slot with nonroot containers. The operator's existing rootful
Docker access is root-equivalent; the validator is an accident boundary for trusted
owned candidates, not OS isolation against a malicious worker or Docker administrator.
No user/group/daemon/firewall changes are made by this tool. Rootless execution and
unattended automation belong to separately bounded U2/U3.

## Consequences and verification

No API/Web lifecycle routes or permanent production UI flags are added. TEST branding
is built into a separate Web image. The installed CLI checks effective Compose, image
metadata and actual containers/resources before start; it records private receipts and
deletes only the exact validated slot resources. Unknown drift fails closed.

Control updates require a newly reviewed snapshot, not candidate-driven replacement.
Read-only control files and hashes detect accidental edits; they do not resist an
operator who can rewrite both the installation and its seal. Artifact metadata is
trusted only after its GitHub repository, owner, run, source/tree and checksums are verified.
Offline tests cannot establish network isolation, runtime compatibility, OCI metadata
truthfulness, client access or owner UX approval. See the
[single TEST runbook](../operations/runbooks/test-environment.md) for those gates.
