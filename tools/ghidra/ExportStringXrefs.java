// Export defined strings matching a case-insensitive regex and their references.
// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Data;
import ghidra.program.model.listing.DataIterator;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.ReferenceIterator;
import java.io.File;
import java.io.PrintWriter;
import java.util.regex.Pattern;

public class ExportStringXrefs extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 2) throw new IllegalArgumentException("output-file regex");
        Pattern pattern = Pattern.compile(args[1], Pattern.CASE_INSENSITIVE);
        try (PrintWriter writer = new PrintWriter(new File(args[0]), "UTF-8")) {
            writer.println("string_address\tvalue\tfrom\tfrom_function_entry\tfrom_function_name");
            DataIterator data = currentProgram.getListing().getDefinedData(true);
            while (data.hasNext() && !monitor.isCancelled()) {
                Data item = data.next();
                Object value = item.getValue();
                if (!(value instanceof String)) continue;
                String text = (String)value;
                if (!pattern.matcher(text).find()) continue;
                ReferenceIterator refs = currentProgram.getReferenceManager().getReferencesTo(item.getAddress());
                boolean any = false;
                while (refs.hasNext()) {
                    any = true;
                    Reference ref = refs.next();
                    Function function = getFunctionContaining(ref.getFromAddress());
                    writer.printf("%s\t%s\t%s\t%s\t%s%n", item.getAddress(), text.replace('\t',' '),
                        ref.getFromAddress(), function == null ? "" : function.getEntryPoint(),
                        function == null ? "" : function.getName());
                }
                if (!any) writer.printf("%s\t%s\t\t\t%n", item.getAddress(), text.replace('\t',' '));
            }
        }
    }
}
