"""Independent original actor replay for timestamped changes between render/tick boundaries."""
import argparse, hashlib, json, subprocess
from pathlib import Path
from door_actor_reference import TABLE_SHA, LEAF_SHA, MEMBER, LAYOUT_SHA, f32, load_reference, ReferenceDoorActor, close

CHANGES = [(137_000, 1.6, True), (1_213_000, -1.6, True), (2_419_000, 1.6, False),
           (2_817_000, -1.6, False), (3_007_000, 1.6, True), (4_113_000, 0, None)]

def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('root', 'configuration', 'private_output', 'core_dll'): p.add_argument(name, type=Path)
    a = p.parse_args()
    a.private_output.resolve().relative_to((Path(__file__).resolve().parents[2] / 'local-private').resolve())
    a.private_output.mkdir(parents=True, exist_ok=True)
    reference = load_reference(a.root, a.configuration)
    command = ['dotnet', str(a.core_dll), '--input-timeline', str(a.root), '--configuration', str(a.configuration),
               '--member', MEMBER, '--expected-sha256', LAYOUT_SHA, '--cell', '3', '--portal', '10',
               '--table-sha256', TABLE_SHA, '--leaf-sha256', LEAF_SHA]
    run = subprocess.run(command, capture_output=True, text=True, timeout=90)
    (a.private_output / 'native-timeline.txt').write_text(run.stdout + run.stderr, encoding='utf-8')
    run.check_returncode()
    def records(prefix): return [json.loads(line[len(prefix):]) for line in run.stdout.splitlines() if line.startswith(prefix)]
    assert {s['Path']: s['Sha256'] for s in records('timeline_source=')} == reference['pins']
    rows = records('timeline_update='); updates = ticks = transfers = 0
    for pattern, partition in [('100hz', [10_000]), ('20hz', [50_000]), ('jitter', [7_000, 11_000, 23_000, 41_000])]:
        for transformed in [False, True]:
            actor = ReferenceDoorActor(reference, transformed); now = frame = render = index = 0; state = None
            samples = [r for r in rows if r['pattern'] == pattern and r['transformed'] == transformed]
            while now < 4_400_000:
                now = min(4_400_000, now + partition[render % len(partition)]); render += 1
                row = samples[index]; index += 1; end_frame = now // 20_000
                for tick in range(frame, end_frame):
                    speed, allowed = 0, True
                    for timestamp, value, permit in CHANGES:
                        if timestamp <= tick * 20_000: speed, allowed = value, permit
                    delta = f32(f32(speed) * f32(.02)); movement = [0, 0, delta] if transformed else [-delta, 0, 0]
                    state = actor.advance(.02, movement, allowed); ticks += 1
                queued = sum(timestamp > end_frame * 20_000 for timestamp, _, _ in CHANGES)
                assert (row['now'], row['steps'], row['frame'], row['pending'], row['queued']) == (now, end_frame-frame, end_frame, now % 20_000, queued)
                if state is not None:
                    for source, key in [('transfers','total'),('cell','cell'),('retained','retained'),('status','status'),('kind','kind')]:
                        assert row[source] == state[key], (pattern, transformed, now, source, row[source], state[key])
                    close(row['local'], state['local']); close(row['world'], state['world'])
                    close(row['fraction'], state['fraction'], 1e-6); close(row['curve'], state['curve'], 1e-6)
                frame = end_frame; updates += 1
            assert index == len(samples) and frame == 220
            transfers += state['total']
    summary = dict(configuration_sha256=hashlib.sha256(a.configuration.read_bytes()).hexdigest().upper(),
                   source_pins=reference['pins'], render_patterns=3, owner_poses=2, input_changes=len(CHANGES),
                   clock_updates=updates, independent_ticks=ticks, transfers=transfers)
    (a.private_output/'verified-summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
    print('PASS: independent timestamped actor replay ' + json.dumps(summary))

if __name__ == '__main__': main()
