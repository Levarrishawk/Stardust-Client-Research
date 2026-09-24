// @category Stardust
import ghidra.app.decompiler.DecompInterface;
import ghidra.app.decompiler.DecompileResults;
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionManager;
import java.io.BufferedWriter;
import java.io.File;
import java.io.FileWriter;

public class DecompileRawEntry extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 2) throw new IllegalArgumentException("entry output-file");
        Address entry = toAddr(args[0].replaceFirst("^(0x|0X)", ""));
        FunctionManager manager = currentProgram.getFunctionManager();
        Function function = manager.getFunctionAt(entry);
        if (function == null) function = createFunction(entry, "provisional_" + args[0]);
        if (function == null) throw new IllegalStateException("Cannot create transient function at " + entry);
        DecompInterface decompiler = new DecompInterface();
        decompiler.openProgram(currentProgram);
        DecompileResults result = decompiler.decompileFunction(function, 120, monitor);
        try (BufferedWriter writer = new BufferedWriter(new FileWriter(new File(args[1])))) {
            writer.write("/* entry=" + entry + " name=" + function.getName() + " */\n");
            writer.write(result.decompileCompleted() ? result.getDecompiledFunction().getC() :
                "/* " + result.getErrorMessage() + " */");
        }
        decompiler.dispose();
    }
}
