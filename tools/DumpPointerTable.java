// Dump 32-bit pointers and resolve any target function names.
// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import java.io.File;
import java.io.PrintWriter;

public class DumpPointerTable extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 3) throw new IllegalArgumentException("start count output-file");
        Address cursor = toAddr(args[0]);
        int count = Integer.parseInt(args[1]);
        try (PrintWriter writer = new PrintWriter(new File(args[2]), "UTF-8")) {
            for (int i = 0; i < count; i++, cursor = cursor.add(4)) {
                long raw = Integer.toUnsignedLong(getInt(cursor));
                Address target = toAddr(String.format("%08x", raw));
                Function function = getFunctionAt(target);
                writer.printf("%s\t%s\t%s%n", cursor, target,
                    function == null ? "" : function.getName(true));
            }
        }
    }
}
