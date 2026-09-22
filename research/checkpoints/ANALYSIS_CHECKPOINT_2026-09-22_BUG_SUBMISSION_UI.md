# Bug-submission UI and ticket bridge

Date: 2026-09-22

## Result

`CuiBugSubmissionPage` is a legacy beta-era bug-report composer with two output
routes: an auxiliary external report/mail helper and, when customer-service
categories are available/configured, the same `CreateTicketMessage` used by the
ticket UI. Its category combo inputs are flattened into a human-readable report
body; they are not a new network schema.

## Principal functions

| Address | Provisional name | Role |
|---|---|---|
| `0x013B88B0` | `cui_bug_submission_construct_VA_013B88B0` | Constructs page, binds Send/Cancel, repeatability, bug type/subtype, game system, severity and description controls; registers category response listener. |
| `0x013B8D30` | `cui_bug_submission_activate_VA_013B8D30` | Activates input context, requests customer-service categories and populates when available. |
| `0x013B8E40` | `cui_bug_submission_deactivate_VA_013B8E40` | Removes input context and deactivates the page. |
| `0x013B8F20` | `cui_bug_submission_button_handler_VA_013B8F20` | Cancel closes; Send validates fields, composes report text, invokes configured report routes, and closes on success. |
| `0x013B9A20` | `cui_bug_submission_append_selection_text_VA_013B9A20` | Appends selected combo text into retained report metadata. |
| `0x013B9E80` | `cui_bug_submission_categories_received_VA_013B9E80` | Installs category snapshot or shows querying/disabled state. |

## Composed body

The Send path requires a description plus the required combo selections and
builds one Unicode report containing labeled lines for:

- Bug Type
- Repeatable
- Game System
- Severity
- free-form retained metadata/description

The executable still contains beta-specific subject/address strings such as
`%s Bug: %s`, a StationId-derived address and
`swgbetatestbugs@soe.sony.com`. These identify obsolete auxiliary reporting
infrastructure; they are not Stardust server endpoints and must not be carried
forward as live destinations.

## CreateTicketMessage bridge

When the customer-service category vector is non-empty and the corresponding
feature flags permit it, the handler obtains the local avatar name and selected
category/subcategory numeric IDs, then calls message constructor `0x00ADFA90`.
The arguments observed at this call site are:

1. local avatar name (ASCII)
2. selected category ID (U32)
3. selected subcategory ID (U32)
4. composed Unicode bug body
5. empty Unicode legacy field
6. empty Unicode legacy field
7. auxiliary narrow metadata produced by the legacy reporting helper
8. Boolean true

This narrows the formerly anonymous tail: the two Unicode tail fields are
explicitly empty in the bug path, the ASCII field is auxiliary report metadata,
and the flag distinguishes this special submission path. Exact original member
names are still not claimed. Current Stardust consumes only fields 1-4.

## Replacement-client rule

Do not reproduce the hard-coded SOE beta email route. If bug reporting is wanted,
make it an explicitly configured modern service. For Stardust interoperability,
reuse the optional customer-service capability and the four-field compatible
ticket prefix; preserve legacy tail widths only when byte-compatible emission is
required. Never send diagnostics externally without clear user action and an
explicit configured destination.

## Confidence

- UI inputs, validation and report-body labels: **Confirmed**.
- Customer-service category and CreateTicketMessage bridge: **Confirmed**.
- Two empty Unicode tail fields, auxiliary ASCII metadata, final true flag at
  this call site: **Confirmed**.
- Exact historical field/member names and obsolete helper transport behavior:
  **Unknown/non-required; do not re-enable**.
