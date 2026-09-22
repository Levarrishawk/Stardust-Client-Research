// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import java.io.*;

public class ExportXrefs extends GhidraScript {
    public void run() throws Exception {
        String[] a = getScriptArgs();
        if (a.length != 2) throw new IllegalArgumentException("address-list output-tsv");
        try (BufferedReader r = new BufferedReader(new FileReader(a[0])); BufferedWriter w = new BufferedWriter(new FileWriter(a[1]))) {
            w.write("target\tfrom\ttype\tfrom_function_entry\tfrom_function_name\n");
            String s;
            while ((s = r.readLine()) != null) {
                s=s.trim(); if (s.isEmpty() || s.startsWith("#")) continue;
                Address target=toAddr(s.replaceFirst("^(0x|0X)", ""));
                ReferenceIterator refs=currentProgram.getReferenceManager().getReferencesTo(target);
                while(refs.hasNext()) {
                    Reference ref=refs.next(); Function f=getFunctionContaining(ref.getFromAddress());
                    w.write(target+"\t"+ref.getFromAddress()+"\t"+ref.getReferenceType()+"\t"+
                        (f==null?"":f.getEntryPoint())+"\t"+(f==null?"":f.getName())+"\n");
                }
            }
        }
    }
}
