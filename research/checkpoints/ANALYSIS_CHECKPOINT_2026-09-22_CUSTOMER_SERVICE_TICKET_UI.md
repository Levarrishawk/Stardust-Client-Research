# Customer-service ticket UI

Date: 2026-09-22

## Result

`SwgCuiTicketSubmission` composes an instructions page and a new-ticket page.
The child view is driven by `CustomerServiceManager` category and ticket-list
snapshots, validates category/subcategory/comment input, enforces the advertised
ticket limit, sends the already mapped `CreateTicketMessage`, and imposes a
60-second local submission cooldown.

## Root shell

| Address | Provisional name | Role |
|---|---|---|
| `0x00F90DE0` | `swg_cui_ticket_submission_construct_VA_00F90DE0` | Constructs Instructions and NewTicket children; binds bug, harassment, Done and two tabs. |
| `0x00F91080` | `swg_cui_ticket_submission_destroy_VA_00F91080` | Releases both children and controls. |
| `0x00F91130` | `swg_cui_ticket_submission_activate_VA_00F91130` | Activates selected child and modal/input state. |
| `0x00F91160` | `swg_cui_ticket_submission_deactivate_VA_00F91160` | Restores input and deactivates children. |
| `0x00F91190` | `swg_cui_ticket_submission_button_handler_VA_00F91190` | Done closes; bug and harassment route to their dedicated flows, with ticket availability/limit gating. |
| `0x00F91300` | `swg_cui_ticket_submission_factory_VA_00F91300` | Loads `/PDA.Service.ticketSubmission`. |
| `0x00F913A0` | `swg_cui_ticket_submission_tab_changed_VA_00F913A0` | Activates exactly one of the two children. |

## New-ticket child

| Address | Provisional name | Role |
|---|---|---|
| `0x01099D30` | `swg_cui_new_ticket_construct_VA_01099D30` | Resolves Submit, topic, subtopic and comments controls and installs customer-service response subscriptions. |
| `0x0109A0D0` | `swg_cui_new_ticket_activate_VA_0109A0D0` | Requests/waits for categories and tickets, populates controls and gates availability against ticket limit. |
| `0x0109A380` | `swg_cui_new_ticket_topic_changed_VA_0109A380` | Rebuilds subtopics for the selected topic. |
| `0x0109A4A0` | `swg_cui_new_ticket_submit_VA_0109A4A0` | Validates ticket count and trimmed comments, obtains avatar/category/subcategory data, sends CreateTicketMessage and starts 60-second cooldown. |
| `0x0109A6A0` | `swg_cui_new_ticket_categories_received_VA_0109A6A0` | Installs/populates returned categories or preserves querying/disabled presentation. |
| `0x0109A870` | `swg_cui_new_ticket_refresh_subtopics_VA_0109A870` | Selects the chosen category's subcategory slice. |
| `0x0109A8F0` | `swg_cui_new_ticket_populate_combo_VA_0109A8F0` | Converts category records into combo entries retaining numeric IDs. |
| `0x0109AA10` | `swg_cui_new_ticket_update_VA_0109AA10` | Decrements submission cooldown and refreshes submit-button eligibility. |

## End-to-end boundary

The submit path calls the previously identified `0x005CAF30`, which constructs
and reliably sends `CreateTicketMessage` (`0x40E64DAC`). Current Stardust consumes
the compatible prefix:

1. ASCII player name
2. U32 category ID
3. U32 subcategory ID
4. Unicode body

Legacy tail fields remain serialized by the original client but are ignored by
the current server. Category-list, existing-ticket and create/cancel/activity
responses are published through `CustomerServiceManager`; the UI does not parse
their wire buffers directly.

The client-side ticket count and cooldown are UX/rate-limit aids only. Stardust
must remain authoritative and may disable the Holocron/customer-service backend.
A replacement must surface that disabled state rather than pretending a ticket
was accepted.

## Replacement-client contract

Treat customer service as an optional capability. Keep its manager/API isolated
from gameplay state, reproduce the four-field compatible create prefix and
existing response codecs, load category IDs from the server response, and retain
legacy tail fields only for byte compatibility. Bug and harassment routes remain
distinct workflows.

## Confidence

- UI composition, validation, ID selection and 60-second cooldown: **Confirmed**.
- UI-to-CreateTicketMessage path: **Confirmed**.
- Current server prefix and optional backend behavior: **Confirmed by source**.
- Historical business names of ignored legacy tail fields: **Unknown/non-material
  for current Stardust interoperability**.
