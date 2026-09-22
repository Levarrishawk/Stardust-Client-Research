// @category Stardust
import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import java.io.*;

public class DecompileAddressList extends GhidraScript {
    public void run() throws Exception {
        String[] a = getScriptArgs();
        if (a.length != 2) throw new IllegalArgumentException("address-list output-dir");
        File dir = new File(a[1]); dir.mkdirs();
        DecompInterface d = new DecompInterface(); d.openProgram(currentProgram);
        try (BufferedReader r = new BufferedReader(new FileReader(a[0]))) {
            String s;
            while ((s = r.readLine()) != null) {
                s = s.trim(); if (s.isEmpty() || s.startsWith("#")) continue;
                Function f = getFunctionAt(toAddr(s.replaceFirst("^(0x|0X)", "")));
                if (f == null) { println("No function at " + s); continue; }
                DecompileResults result = d.decompileFunction(f, 120, monitor);
                File out = new File(dir, f.getEntryPoint() + "_" + f.getName() + ".c");
                try (BufferedWriter w = new BufferedWriter(new FileWriter(out))) {
                    w.write("/* entry=" + f.getEntryPoint() + " name=" + f.getName() + " */\n");
                    w.write(result.decompileCompleted() ? result.getDecompiledFunction().getC() : "/* " + result.getErrorMessage() + " */");
                }
            }
        }
        d.dispose();
    }
}
