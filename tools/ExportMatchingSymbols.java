// Export symbols and containing functions whose names match a case-insensitive regex.
// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolIterator;
import java.io.File;
import java.io.PrintWriter;
import java.util.regex.Pattern;

public class ExportMatchingSymbols extends GhidraScript {
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 2) {
            throw new IllegalArgumentException("usage: <output-file> <regex>");
        }
        Pattern pattern = Pattern.compile(args[1], Pattern.CASE_INSENSITIVE);
        File output = new File(args[0]);
        try (PrintWriter writer = new PrintWriter(output, "UTF-8")) {
            SymbolIterator symbols = currentProgram.getSymbolTable().getAllSymbols(true);
            while (symbols.hasNext() && !monitor.isCancelled()) {
                Symbol symbol = symbols.next();
                String name = symbol.getName(true);
                if (!pattern.matcher(name).find()) continue;
                Address address = symbol.getAddress();
                Function function = currentProgram.getFunctionManager().getFunctionContaining(address);
                writer.printf("%s\t%s\t%s\t%s%n", address, symbol.getSymbolType(), name,
                    function == null ? "" : function.getName(true));
            }
        }
    }
}
