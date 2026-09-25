// Export CALL instructions whose operand contains a requested scalar displacement.
// Discovery aid only: a matching displacement does not prove receiver type.
// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.listing.Instruction;
import ghidra.program.model.scalar.Scalar;
import java.io.File;
import java.io.PrintWriter;

public class ExportIndirectCallsByDisplacement extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 2) {
            throw new IllegalArgumentException("usage: <displacement> <output-file>");
        }
        long wanted = Long.decode(args[0]);
        try (PrintWriter writer = new PrintWriter(new File(args[1]), "UTF-8")) {
            writer.println("call\tdisassembly\towner_entry\towner_name");
            for (Instruction instruction : currentProgram.getListing().getInstructions(true)) {
                if (monitor.isCancelled()) break;
                if (!"CALL".equalsIgnoreCase(instruction.getMnemonicString())) continue;
                boolean match = false;
                for (int operand = 0; operand < instruction.getNumOperands() && !match; operand++) {
                    for (Object object : instruction.getOpObjects(operand)) {
                        if (object instanceof Scalar && ((Scalar)object).getSignedValue() == wanted) {
                            match = true;
                            break;
                        }
                    }
                }
                if (!match) continue;
                Function owner = getFunctionContaining(instruction.getAddress());
                writer.printf("%s\t%s\t%s\t%s%n", instruction.getAddress(), instruction,
                    owner == null ? "" : owner.getEntryPoint(),
                    owner == null ? "" : owner.getName());
            }
        }
    }
}
