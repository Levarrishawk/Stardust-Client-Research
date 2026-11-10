from pathlib import Path
import shutil
root=Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
backup=Path(r'C:\Stardust-Experimental\diagnostics\g7-modal-chat-scene-defaults')
backup.mkdir(exist_ok=False)
edits=[
 ('src/engine/client/library/clientUserInterface/src/shared/core/CuiPreferences.cpp',
  '\t// Seed original-profile defaults before loading saved user choices.',
  '\t// Seed scene-specific original-profile defaults before loading saved user choices.'),
 ('src/engine/client/library/clientUserInterface/src/shared/core/CuiPreferences.cpp',
  '\t\tms_modalChat[Game::ST_space] = false;',
  '\t\tms_modalChat[Game::ST_space] = true;'),
 ('src/engine/client/library/clientGame/src/shared/core/InputScheme.cpp',
  '\t\tCuiPreferences::setModalChat        ( !precuChat && (( flags & F_modalChat ) || ( flags & F_modeless )) );',
  '\t\tconst bool modalChat = precuChat ? (sceneType == Game::ST_space) : ((flags & F_modalChat) || (flags & F_modeless));\n\t\tCuiPreferences::setModalChat(modalChat);'),
]
pending={}
for name,old,new in edits:
    p=root/name
    if p not in pending:
        shutil.copy2(p,backup/p.name)
        pending[p]=p.read_text(encoding='utf-8')
    if pending[p].count(old)!=1:raise RuntimeError('Expected one edit anchor: '+name)
    pending[p]=pending[p].replace(old,new)
for p,s in pending.items():
    tmp=p.with_name(p.name+'.g7tmp');tmp.write_text(s,encoding='utf-8');tmp.replace(p)
print('Pre-CU new-profile and preset-reset modal defaults: ground off, space on. Saved-option registration remains unchanged.')
