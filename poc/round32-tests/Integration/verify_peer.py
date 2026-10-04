"""Literal independently specified synthetic TANO6 wrap/assignment peer oracle."""
from pathlib import Path
import json,sys
fixture=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8'))
records=json.loads(Path(sys.argv[2]).read_text(encoding='utf-8'))
expected=[dict(Name='seed',**fixture['Seed'])]+fixture['Steps']
assert len(records)==len(expected)==6
for revision,(record,value) in enumerate(zip(records,expected),1):
    assert record['Name']==value['Name']
    assert record['ObjectId']==fixture['ObjectId']
    for key in ('Passive','Counter','Ids'):assert record[key]==value[key],(key,record,value)
    assert record['Revision']==revision
    assert record['DecodedBytes']==8+8*len(value['Ids'])
print('PASS lead TANO6 peer:6 literal states/full IDs/counter wrap/assign/clear')
