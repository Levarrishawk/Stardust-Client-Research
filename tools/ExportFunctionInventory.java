// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.FunctionIterator;
import java.io.BufferedWriter;
import java.io.FileWriter;

public class ExportFunctionInventory extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 1) throw new IllegalArgumentException("Output path required");
        int count = 0;
        try (BufferedWriter out = new BufferedWriter(new FileWriter(args[0]))) {
            out.write("entry\tend\tsize\tname\tcalling_convention\tparameter_count\tthunk\n");
            FunctionIterator functions = currentProgram.getFunctionManager().getFunctions(true);
            while (functions.hasNext()) {
                Function f = functions.next();
                out.write(f.getEntryPoint() + "\t" + f.getBody().getMaxAddress() + "\t" +
                    f.getBody().getNumAddresses() + "\t" + f.getName() + "\t" +
                    f.getCallingConventionName() + "\t" + f.getParameterCount() + "\t" + f.isThunk() + "\n");
                count++;
            }
        }
        println("Exported functions: " + count);
    }
}
