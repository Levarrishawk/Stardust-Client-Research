// @category Stardust
import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import java.io.*;
import java.util.*;

public class DecompileContainingAddressList extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 2) throw new IllegalArgumentException("address-list output-dir");
        File directory = new File(args[1]); directory.mkdirs();
        Set<String> done = new HashSet<>();
        DecompInterface decompiler = new DecompInterface(); decompiler.openProgram(currentProgram);
        try (BufferedReader reader = new BufferedReader(new FileReader(args[0]))) {
            String line;
            while ((line = reader.readLine()) != null) {
                line = line.trim(); if (line.isEmpty() || line.startsWith("#")) continue;
                Function function = getFunctionContaining(toAddr(line.replaceFirst("^(0x|0X)", "")));
                if (function == null) { println("No containing function: " + line); continue; }
                String entry = function.getEntryPoint().toString();
                if (!done.add(entry)) continue;
                DecompileResults result = decompiler.decompileFunction(function, 120, monitor);
                File output = new File(directory, entry + "_" + function.getName() + ".c");
                try (BufferedWriter writer = new BufferedWriter(new FileWriter(output))) {
                    writer.write("/* entry=" + entry + " name=" + function.getName() + " sample=" + line + " */\n");
                    writer.write(result.decompileCompleted() ? result.getDecompiledFunction().getC() : "/* " + result.getErrorMessage() + " */");
                }
            }
        }
        decompiler.dispose();
    }
}
