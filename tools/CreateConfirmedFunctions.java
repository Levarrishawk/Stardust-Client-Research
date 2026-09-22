// @category Stardust
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.SourceType;
import java.io.*;

public class CreateConfirmedFunctions extends GhidraScript {
    public void run() throws Exception {
        String[] a=getScriptArgs();
        if(a.length!=1) throw new IllegalArgumentException("address,name CSV required");
        try(BufferedReader r=new BufferedReader(new FileReader(a[0]))) {
            String s; while((s=r.readLine())!=null) {
                s=s.trim(); if(s.isEmpty()||s.startsWith("#")||s.startsWith("address,")) continue;
                String[] p=s.split(",",2); Address at=toAddr(p[0].replaceFirst("^(0x|0X)",""));
                Function f=getFunctionAt(at);
                if(f==null) f=createFunction(at,p[1]);
                if(f==null) throw new IllegalStateException("Could not create function at "+at);
                f.setName(p[1],SourceType.USER_DEFINED);
                println("Confirmed function "+at+" "+f.getName());
            }
        }
    }
}
