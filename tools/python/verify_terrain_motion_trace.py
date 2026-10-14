"""Independently verify every published terrain proof position against input and triangles.

Input remains synthetic in the host proof. This is not physical-key or legacy movement parity.
"""
import argparse
import json
import math
from pathlib import Path
import struct


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def verify(directory):
    receipt = json.loads((directory / 'terrain-proof.json').read_text())
    def read_geometry(name):
        geometry = json.loads((directory / name).read_text())
        triangles = []
        for patch in geometry:
            points = [tuple(map(f32, point)) for point in patch['Positions']]
            indices = patch['Indices']
            for at in range(0, len(indices), 3):
                a, b, c = (points[i] for i in indices[at:at+3])
                den = (b[2]-c[2])*(a[0]-c[0])+(c[0]-b[0])*(a[2]-c[2])
                assert den != 0
                triangles.append((a, b, c, den))
        return triangles

    windows = {0: read_geometry('terrain-geometry.json')}
    publications = receipt.get('StreamPublications', [])
    if receipt.get('Streaming'):
        assert receipt['Backend'] == 'd3d12' and len(publications) >= 3
        initial = receipt.get('InitialWindow')
        side = 4 if initial is None else initial['Columns']
        assert side in (4, 8)
        if initial is not None:
            assert initial['Rows'] == side and initial['ChunkWidth'] == 40
            ix, iz = initial['FirstX']*40, initial['FirstZ']*40
            assert all(ix <= point[0] <= ix+side*40 and iz <= point[2] <= iz+side*40
                       for triangle in windows[0] for point in triangle[:3])
        count = side*side
        assert len(windows[0]) == count*128
        previous = None
        if receipt.get('InitialResidencyAdopted'):
            assert initial is not None
            previous = {(initial['FirstX']+x, initial['FirstZ']+z) for x in range(side) for z in range(side)}
        last_generation = 0
        for publication in publications:
            generation = publication['Generation']
            assert isinstance(generation, int) and generation > last_generation
            name = f'terrain-geometry-{generation}.json'
            assert publication['GeometryFile'] == name
            triangles = read_geometry(name)
            assert len(triangles) == count*128
            x0, z0 = publication['FirstX']*40, publication['FirstZ']*40
            assert all(x0 <= point[0] <= x0+side*40 and z0 <= point[2] <= z0+side*40
                       for triangle in triangles for point in triangle[:3])
            keys = {(publication['FirstX']+x, publication['FirstZ']+z) for x in range(side) for z in range(side)}
            reused = 0 if previous is None else len(keys & previous)
            assert publication['ReusedChunks'] == reused
            assert publication['BuiltChunks'] == count-reused
            assert publication['ReusedVisuals'] == reused*16
            assert publication['NewVisuals'] == (count-reused)*16
            assert publication['DisposedVisuals'] == (count-reused)*16
            previous, last_generation = keys, generation
            windows[generation] = triangles
        assert sum(row['ReusedChunks'] for row in publications) >= 12

    def height(x, z, generation):
        for a, b, c, den in windows[generation]:
            if not min(a[0], b[0], c[0])-1e-7 <= x <= max(a[0], b[0], c[0])+1e-7:
                continue
            if not min(a[2], b[2], c[2])-1e-7 <= z <= max(a[2], b[2], c[2])+1e-7:
                continue
            u = ((b[2]-c[2])*(x-c[0])+(c[0]-b[0])*(z-c[2]))/den
            v = ((c[2]-a[2])*(x-c[0])+(a[0]-c[0])*(z-c[2]))/den
            w = 1-u-v
            if min(u, v, w) >= -1e-8:
                return u*a[1]+v*b[1]+w*c[1]
        raise AssertionError('Published position is outside rendered triangles')

    trace = receipt['MotionTrace']
    assert 3 <= len(trace) <= 20000 and len(trace) == receipt['ActualProcessUpdates']
    position = list(map(f32, trace[0]['Before']))
    expected_x, expected_z = position[0], position[2]
    distance = ground_error = displacement_error = 0.0
    speeds = set()
    edge_holds = obstacle_holds = 0
    generation = 0
    uncapped = []
    transition_following = []
    wall_deltas = []
    wall_transition_following = []
    previous_generation = 0
    transition_previous_frame = False
    for sample in trace:
        next_generation = sample.get('SurfaceGeneration', 0)
        assert next_generation in windows and next_generation >= generation
        generation = next_generation
        before, after = list(map(f32, sample['Before'])), list(map(f32, sample['After']))
        assert before == position, 'Discontinuous position trace'
        dt = sample['Delta']
        assert math.isfinite(dt) and 0 <= dt <= .1
        if 'UncappedDelta' in sample:
            raw = sample['UncappedDelta']
            assert math.isfinite(raw) and raw >= 0 and dt == min(raw, .1)
            uncapped.append(raw)
            if transition_previous_frame:
                transition_following.append(raw)
        if 'WallFrameDelta' in sample:
            wall = sample['WallFrameDelta']
            if wall is None:
                assert not wall_deltas and sample is trace[0]
            else:
                assert math.isfinite(wall) and wall >= 0
                wall_deltas.append(wall)
                if transition_previous_frame:
                    wall_transition_following.append(wall)
        transition_previous_frame = next_generation != previous_generation
        previous_generation = next_generation
        key_x, key_z = sample['Input']
        assert abs(key_x) <= 1 and abs(key_z) <= 1
        fx, _, fz = map(f32, sample['Forward'])
        length = math.hypot(fx, fz)
        assert length >= 1e-6
        fx, fz = fx/length, fz/length
        key_length = max(1, math.hypot(key_x, key_z))
        key_x, key_z = key_x/key_length, key_z/key_length
        speed = (6 if sample['Run'] else 3) if sample['Active'] and (key_x or key_z) else 0
        held = sample['EdgeHolds'] > edge_holds or sample['ObstacleHolds'] > obstacle_holds
        assert sample['EdgeHolds'] >= edge_holds and sample['ObstacleHolds'] >= obstacle_holds
        edge_holds, obstacle_holds = sample['EdgeHolds'], sample['ObstacleHolds']
        if held:
            assert after == before and sample['Speed'] == 0
        else:
            assert sample['Speed'] == speed
            expected_x += (fz*key_x-fx*key_z)*speed*dt
            expected_z += (-fx*key_x-fz*key_z)*speed*dt
        error = math.hypot(after[0]-expected_x, after[2]-expected_z)
        assert error < .002, 'Published movement disagrees with independently integrated input'
        displacement_error = max(displacement_error, error)
        ground = abs(after[1]-height(after[0], after[2], generation))
        assert ground < 1e-4, 'Published actor is not grounded on rendered triangles'
        ground_error = max(ground_error, ground)
        distance += math.hypot(after[0]-before[0], after[2]-before[2])
        speeds.add(sample['Speed'])
        position = after
    assert {0, 3, 6} <= speeds
    if receipt.get('Streaming'):
        assert generation == publications[-1]['Generation'] and distance > 80
    assert not uncapped or len(uncapped) == len(trace), 'Partial uncapped timing trace'
    ordered = sorted(uncapped)
    timing = None if not ordered else dict(Samples=len(ordered), MaximumSeconds=max(ordered),
        MedianSeconds=ordered[len(ordered)//2], P99Seconds=ordered[min(len(ordered)-1, math.ceil(len(ordered)*.99)-1)],
        FramesOver100ms=sum(value > .1 for value in ordered),
        FollowingPublicationSeconds=transition_following, PhysicalFramePacingAcceptance=False)
    wall_ordered=sorted(wall_deltas)
    wall_timing=None if not wall_ordered else dict(Samples=len(wall_ordered),MaximumSeconds=max(wall_ordered),
        MedianSeconds=wall_ordered[len(wall_ordered)//2],P99Seconds=wall_ordered[min(len(wall_ordered)-1,math.ceil(len(wall_ordered)*.99)-1)],
        FramesOver100ms=sum(value>.1 for value in wall_ordered),FollowingPublicationSeconds=wall_transition_following,
        Clock='MonotonicStopwatchProcessStartIntervals',PhysicalFramePacingAcceptance=False)
    assert not wall_deltas or len(wall_deltas)==len(trace)-1, 'Partial wall-clock timing trace'
    return dict(Passed=True, PublishedPositions=len(trace), MeasuredHorizontalDistance=distance,
                MaximumGroundError=ground_error, MaximumDisplacementError=displacement_error,
                VerifiedStreamingWindows=len(publications),
                UncappedTiming=timing,
                WallFrameTiming=wall_timing,
                PhysicalInputVerified=False, OriginalNumericParity=False)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--proof', type=Path, required=True)
    print(json.dumps(verify(parser.parse_args().proof)))
