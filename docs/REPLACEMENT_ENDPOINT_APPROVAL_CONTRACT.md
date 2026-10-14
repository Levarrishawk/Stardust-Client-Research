# Approved advertised endpoint handoff

Round91 adds [ReplacementEndpointApproval](../../SWG-Godot-Client/poc/Stardust.Core/Session/ReplacementEndpointApproval.cs), an explicit operator mapping for the [application](REPLACEMENT_CLIENT_APPLICATION_CONTRACT.md). It links a received selected galaxy and advertised zone endpoint to the injected pinned peer before acquisition. Mapping presence is not live authorization, DNS validation, server ownership proof or deployed interoperability evidence. No socket, UDP port, DNS query or external change is introduced.

## Immutable policy

Namespace `Stardust.Core.Session`:

```csharp
var approval = new ReplacementEndpointApproval(
    new ReplacementApprovedEndpoint(approvedLoginHost, approvedLoginIpv4, approvedLoginPort),
    new ReplacementApprovedEndpoint(approvedZoneHost, approvedZoneIpv4, approvedZonePort),
    approvedGalaxyId);
// Append endpointApproval: approval to the accepted application constructor.
```

Round92 appends optional `ulong? approvedCharacterId=null` to the Approval constructor and exposes get-only ApprovedCharacterId. A supplied value must be nonzero. ValidateZone also compares the selected ObjectId before zone factory acquisition; old null-policy character binding preserves the accepted API. The strict [controlled launch](REPLACEMENT_CONTROLLED_LAUNCH_CONTRACT.md) always supplies one nonzero approved character. Tests reject a received listed character with the correct endpoint/galaxy when a different character was approved, preserving credentials, selection, clocks and zero zone factory/wire; exact approved-character handoff remains positive. This binds an operator input, not account ownership or live authorization.

ApprovedEndpoint exposes get-only Hostname, Ipv4Address and Port; Approval exposes get-only Login, Zone and GalaxyId. Constructors validate and retain immutable values. Hostname is canonical lowercase ASCII DNS labels, each1..63 bytes, total<=253, letters/digits/hyphens, no leading/trailing hyphen, empty label or trailing dot. URI/path, whitespace/control, underscore, Unicode and IPv6 reject. Numeric literals require canonical four-part decimal IPv4 without shorthand, hexadecimal or leading zeros; their value must equal the supplied IPv4 word. IPv4 word must be nonzero/nonbroadcast and port nonzero. Galaxy ID is an explicit U32, including0 if specifically supplied. Address representation matches DatagramPeer's existing big-endian IPv4 word; this code performs no resolution.

DNS case differences canonicalize; an advertised numeric literal is not treated as an alias for an approved hostname, even when its address matches. This intentionally requires the approved spelling category. Operator must supply the correct mapping; a hostname mapping cannot verify itself without independent evidence. Hostname/IP/port getters hold explicit configuration and should not be logged. Policy/endpoint/selection ToString and rejection messages contain only redacted state/reason text.

## Preflight before acquisition

Application constructor appends optional `ReplacementEndpointApproval? endpointApproval=null` after its existing zeroPolicy. With a policy it compares options.Host/options.Port and pinned login peer IPv4/port against Login before original preparation or port factory acquisition. A mismatch throws ordinary argument rejection before any owned startup work. Existing startup rollback behavior remains unchanged for later preparation/factory/owner failures.

`ReplacementClientSession.PreflightZone(ulong objectId,uint zoneConnectionId)` is an owner-thread, nonreentrant, credential-free observation. It requires CharacterList, nonzero requested object, zone connection distinct from login, globally unique character object IDs, exactly one requested character and exactly one received cluster and status for its galaxy. It applies the existing bounded advertised endpoint grammar and returns immutable `ReplacementZoneSelection` with ObjectId, GalaxyId and Endpoint. Preflight does not copy credentials, retire login, queue packets, select a character or advance the session clock. It exposes no credential blob. BeginZone reuses the same private grammar; the existing copy/retirement/handshake happens only in BeginZone after that selection grammar succeeds.

When policy is present, application SelectCharacter invokes PreflightZone and compares selected GalaxyId, canonical advertised hostname and **ZonePort**, and fixed zone peer address/port to Zone. PingPort is ignored; using the ping port as the approved fixed zone peer rejects. These checks run before zone factory invocation and outside its terminal failure handler. Rejection preserves CharacterList, login, credential length, selected identity0, selection submission count0, run generation0 and both clocks. An accepted preflight returns only an observation; it does not authorize calling live services.

Duplicate cluster/status IDs can reject earlier in the existing login receive owner, faulting/retiring the login; they still acquire no zone port and send no zone credentials/selection. Duplicate characters and missing matching records reject in preflight without retiring a healthy received list. Existing factory/admission failure after approval still retires the application and every returned port according to its ownership contract.

Without approval the optional constructor preserves accepted explicitly injected synthetic behavior, including its existing factory/admission order. It does not establish advertised-to-pinned correspondence and must not be promoted to a controlled live composition. Caller must deliberately supply approval for the future controlled scenario. Trusted injected factory must obey existing owner constraints; caller changing borrowed session state during factory execution is outside this handoff scenario. No credentials are stored in the approval.

## Verification and next boundary

[Own native tests](../../SWG-Godot-Client/poc/round91-tests/Runtime/Runtime.csproj) combine accepted synthetic installation preparation with independently authored protected login lists. Nineteen endpoint cases cover matching casefold/literal mapping, ignored ping field, wrong hostname/literal/fixed IPv4/zone port/galaxy, ping confusion, duplicate/missing records and malformed advertised forms. Four complete handoffs compare exact protected credential and selection bytes. Fifteen rejected cases acquire no zone port and send no zone credentials/selection. Malformed mapping and login startup mismatch reject before factory; rejection messages are checked for endpoint/credential echoes. A rejected future timestamp followed by an earlier valid Tick verifies that preflight consumes neither run nor session clock. Every case performs owner-thread application drain/finalization and exclusive synthetic archive reopening.

CLI: `fixtures.py --out JSON`, `Runtime.dll JSON`, `verify.py --fixtures JSON --result LOG --out JSON`. Fresh ignored paths and accepted offline .NET cache are required. Confirmed evidence is bounded modern mapping/ownership and exact authored wire behavior; no original executable address claims or successful original resource publication are added. Operator identity, deployed hostname mapping, chosen-character baseline/resource limits, validated position/passable floor and live server authority remain separate unverified prerequisites. Next: independently replay adversarial handoffs and supply an approved mapping to the engine adapter before considering any specific live authorization.
