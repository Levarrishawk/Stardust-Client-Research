# Analysis Checkpoint: Random Character Name Request

## Outcome

The already documented `ClientRandomNameResponse` transaction is now complete on its request side. `ClientRandomNameRequest` (`0xD6D1B6D1`) contains one ASCII race/template path and is sent reliably by avatar creation.

## Route

```text
avatar creation random-name action
  -> request preparation 0x00BB6670
  -> current creature template/race path from 0x00B23C40
  -> ClientRandomNameRequest constructor 0x00AE5E60
  -> reliable send 0x00462670
  -> ClientRandomNameRequest callback / 0xD6D1B6D1
  -> parse ASCII raceFile
  -> map template substring to species
  -> NameManager::makeCreatureName, uniqueness retry (maximum 20)
  -> ClientRandomNameResponse / 0xE85FB868
```

The request does not transmit a numeric species ID. Original asset/template naming is part of this protocol behavior: the server derives species by substring matching `wookiee`, `bothan`, `ithorian`, `moncal`, `rodian`, `sullustan`, `trandoshan`, `twilek`, or `zabrak`, defaulting to human. A replacement client must therefore send the original-compatible creature template path selected by character creation.

## Confidence

Identity, opcode, single ASCII field, reliable-send path, server species mapping and paired response: **Confirmed**.

