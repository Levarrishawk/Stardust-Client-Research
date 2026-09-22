// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Instruction;
import java.io.*;

public class ExportDisassemblyRange extends GhidraScript {
    public void run() throws Exception {
        String[] a = getScriptArgs();
        if (a.length != 3) throw new IllegalArgumentException("start end output");
        Address start = toAddr(a[0].replaceFirst("^(0x|0X)", ""));
        Address end = toAddr(a[1].replaceFirst("^(0x|0X)", ""));
        try (BufferedWriter w = new BufferedWriter(new FileWriter(a[2]))) {
            Instruction i = getInstructionAt(start);
            if (i == null) i = getInstructionAfter(start);
            while (i != null && i.getAddress().compareTo(end) < 0) {
                w.write(i.getAddress() + "\t" + i.toString() + "\n");
                i = i.getNext();
            }
        }
    }
}
