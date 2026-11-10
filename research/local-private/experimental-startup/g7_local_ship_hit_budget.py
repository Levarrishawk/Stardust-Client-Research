from pathlib import Path
import shutil
root = Path(r'C:\Users\ancie\Documents\GitHub\SD-Src-Client')
p = root / 'src/engine/client/library/clientGame/src/shared/controller/ShipController.cpp'
backup = Path(r'C:\Stardust-Experimental\diagnostics\g7-local-hit-budget')
backup.mkdir(exist_ok=False)
shutil.copy2(p, backup / p.name)
s = p.read_text(encoding='utf-8')
old = '''				static unsigned count = 0;
				if (count < 64)
				{
					ShipObject const * const ship = getShipOwner();
'''
new = '''				static unsigned count = 0;
				static unsigned localCount = 0;
				static unsigned remoteCount = 0;
				ShipObject const * const ship = getShipOwner();
				bool const local = ship && ship == Game::getPlayerContainingShip();
				if (local ? localCount < 64 : remoteCount < 32)
				{
'''
assert s.count(old) == 1
s = s.replace(old, new, 1)
old = '							ship && ship == Game::getPlayerContainingShip() ? 1 : 0,'
assert s.count(old) == 1
s = s.replace(old, '							local ? 1 : 0,', 1)
anchor = '						std::fclose(file);'
idx = s.index(anchor, s.index('diagnostics/precu-ship-hit.log'))
s = s[:idx] + '						if (local)\n							++localCount;\n						else\n							++remoteCount;\n' + s[idx:]
t = p.with_name(p.name + '.g7tmp')
t.write_text(s, encoding='utf-8')
t.replace(p)
print('Separated bounded local64/remote32 ship-hit diagnostic allowances.')
