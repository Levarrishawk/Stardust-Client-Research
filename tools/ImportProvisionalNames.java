// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.SourceType;
import java.io.BufferedReader;
import java.io.FileReader;

public class ImportProvisionalNames extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 1) throw new IllegalArgumentException("CSV path required");
        int functions = 0, labels = 0, skipped = 0;
        try (BufferedReader reader = new BufferedReader(new FileReader(args[0]))) {
            String line;
            while ((line = reader.readLine()) != null) {
                line = line.trim();
                if (line.isEmpty() || line.startsWith("#") || line.startsWith("address,")) continue;
                String[] fields = line.split(",", 2);
                if (fields.length != 2) { skipped++; continue; }
                Address address = toAddr(fields[0].replaceFirst("^(0x|0X)", ""));
                String name = fields[1].trim().replaceAll("[^A-Za-z0-9_.$@?]", "_");
                Function function = getFunctionAt(address);
                if (function != null) {
                    function.setName(name, SourceType.USER_DEFINED);
                    functions++;
                } else {
                    createLabel(address, name, true, SourceType.USER_DEFINED);
                    labels++;
                }
            }
        }
        println("Imported provisional names: functions=" + functions + " labels=" + labels + " skipped=" + skipped);
    }
}
