// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.symbol.ReferenceIterator;

public class FindAddressReferences extends GhidraScript {
    public void run() throws Exception {
        for (String raw : getScriptArgs()) {
            Address target = toAddr(raw);
            println("TARGET\t" + target);
            ReferenceIterator refs = currentProgram.getReferenceManager().getReferencesTo(target);
            while (refs.hasNext()) {
                var ref = refs.next();
                var function = currentProgram.getFunctionManager().getFunctionContaining(ref.getFromAddress());
                println("REF\t" + ref.getFromAddress() + "\t" +
                    (function == null ? "<no-function>" : function.getEntryPoint() + "\t" + function.getName()));
            }
        }
    }
}
