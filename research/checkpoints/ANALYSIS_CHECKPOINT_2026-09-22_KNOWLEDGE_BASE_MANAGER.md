# Knowledge-Base Manager and Service Split

## Conclusion

The executable contains two distinct knowledge-base systems which must not be conflated:

1. `CuiKnowledgeBaseManager` loads the shipped offline help hierarchy from original SWG data-table assets.
2. The customer-service listener handles server search/article responses for the service UI.

The first is an asset-compatibility requirement. The second is an optional server workflow which current Stardust registers but does not actually implement in its queue-command handlers.

## Offline asset-backed manager

`cui_knowledge_base_manager_install_VA_00A110C0` creates the root node, initializes the node/type constants, and calls `cui_knowledge_base_load_file_list_VA_00A11510`.

The loader opens:

```text
datatables/knowledgebase/filelist.iff
```

Every row names another table below `datatables/knowledgebase/`. The effective patch corpus names 22 shipped sections: `begin`, `navigation`, `communication`, `interface`, `buildings`, `grouping`, `combat`, `healing`, `crafting`, `resources`, `advancement`, `missions`, `professions`, `player_cities`, `planets`, `bestiary`, `terms`, `faq`, `mounts`, `tradein`, `housing`, and `space`.

`cui_knowledge_base_load_section_VA_00A11740` consumes five columns per row. Server-side SQL mirrors the deployed data and identifies their semantics:

| Column | Meaning |
|---|---|
| 0 | dotted parent path |
| 1 | child/node name |
| 2 | node type discriminator |
| 3 | primary localized text, resource, command, or link value |
| 4 | auxiliary value used by button/message variants |

RTTI exposes the concrete client node family:

- `BaseKBNode`
- `StringKBNode`
- `PageKBNode`
- `ImageKBNode`
- `CommandButtonKBNode`
- `LinkButtonKBNode`
- `ScriptMessageButtonKBNode`

The loader resolves the dotted parent path, constructs the corresponding node subclass, and appends it to the parent's child collection. Empty parent denotes the root. The configuration flag `fatalOnBadKnowledgeBaseEntry` controls the strictness of malformed entries.

This is a strong original-asset dependency: a replacement should implement the table schema and node model, then render it in the new UI framework. It should not hard-code the 22 current sections or transcribe their localized content.

## Online customer-service path

The already mapped network paths remain separate:

| Message | Opcode | Client result |
|---|---:|---|
| `SearchKnowledgeBaseResponseMessage` | `0x7CBC8F67` | status plus vector of Unicode title, ASCII article ID, and a trailing U16 compatibility field; publish typed search-results event |
| `GetArticleResponseMessage` | `0x934BAEE0` | status plus Unicode article body; publish typed article-result event |

Current Stardust registers `csSearchKnowledgeBase` and `csGetArticle`, but both command implementations only validate state/locomotion and return `SUCCESS`; they do not query a database and do not construct either response. Packet constructors remain present, while the old `knowledgebase` SQL table is archived/unused. Therefore this online path is dormant in the current server.

The U16 appended per search hit is written as zero by the available server constructor and retained by the client result record. Its historical semantic label remains unknown, but its width/order are confirmed and it is not needed to reproduce current Stardust behavior.

## Replacement boundary

- The offline knowledge base belongs to the asset/resource and UI layers.
- Customer-service search/article messages belong to the protocol layer and may return an explicit unsupported/empty result until the server supplies content.
- Preserve data-table strings and localized StringId references; do not convert them into compiled UI text.
- Node ownership should use ordinary x64-safe containers and IDs. No legacy node pointer or STL layout is serialized.
- Loading must respect TRE search-path overlay priority, including Stardust's replacement `terms.iff`.

## Confidence

- Offline file-list path, per-section loading, five-column access, node class family: **Confirmed by executable and RTTI**.
- Column semantics and 22-section corpus: **Confirmed by matching deployed assets and server data-table SQL**.
- Online message layouts and client publication: **Confirmed**.
- Current server online handlers being operational no-ops: **Confirmed by source**.
- Historical meaning of search-result trailing U16: **Unknown, wire position confirmed**.
