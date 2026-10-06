using System;
using System.Linq;
using System.Text.Json;
using Godot;
using Stardust.Core.Protocol;
using Stardust.Core.Session;

public partial class ScenePumpedWorldPreview
{
    private SceneWorldSoeApplicationIngress? soeApplicationIngress;
    private int soeIngressControls;
    private void PrepareSoeIngress()
    {
        if(pointCameraRelativeHosted)soeApplicationIngress=new(world!,pump!,runTransport!,2,124);
    }
    private void QueueSoePointCorrection()
    {
        var profile=pointProfile!.RootElement;bool malformed=profile.GetProperty("MalformedSoeCorrection").GetBoolean();
        byte[] application=Convert.FromHexString(profile.GetProperty("SoeCorrectionHex").GetString()!);
        if(malformed)Array.Resize(ref application,application.Length-1);
        pointQueuedHex=Convert.ToHexString(application);pointCorrectionPending=true;
        try {
            var inputs=profile.GetProperty("SoeCorrectionDatagrams");
            for(int sequence=0;sequence<inputs.GetArrayLength();sequence++){
                byte[] datagram=Convert.FromHexString(inputs[sequence].GetString()!);
                SceneWorldSoeApplicationIngressReceipt? receipt=null;
                try {
                    receipt=soeApplicationIngress!.Receive(generation,datagram);
                    var controls=receipt.ControlDatagrams.Select(Convert.ToHexString).ToArray();
                    Check(receipt.Accepted&&receipt.QueuedApplications==1&&receipt.QueuedBytes==application.Length
                        &&controls.Length==1&&controls[0]==profile.GetProperty("SoeCorrectionAcks")[sequence].GetString(),"Actual offline SOE correction queues one copied application and independent unsent control");
                    foreach(var control in receipt.ControlDatagrams)Array.Clear(control);
                    soeIngressControls+=controls.Length;
                    GD.Print("soe_point_correction_ingress="+JsonSerializer.Serialize(new{AtStep=pointIndex,Sequence=sequence,Hex=Convert.ToHexString(datagram),
                        QueuedApplications=receipt.QueuedApplications,QueuedMessages=pump!.QueuedMessageCount,QueuedBytes=pump.QueuedMessageBytes,ControlHex=controls,
                        RejectedFraming=receipt.Status==SceneWorldSoeApplicationIngressStatus.RejectedFraming,CallerControlsCleared=true,Sent=false}));
                }finally{Array.Clear(datagram);if(receipt is not null)foreach(var control in receipt.ControlDatagrams)Array.Clear(control);}
            }
        }finally{Array.Clear(application);}
        pointCorrectionQueued=true;
        GD.Print("point_correction_queued="+JsonSerializer.Serialize(new{AtStep=pointIndex,Hex=pointQueuedHex,Messages=pump!.QueuedMessageCount,Bytes=pump.QueuedMessageBytes,CallerBytesCleared=true,Sent=false,OriginalMovementParity=false}));
    }
    private void CloseSoeIngress()
    {
        var ingress=soeApplicationIngress;soeApplicationIngress=null;if(ingress is null)return;
        ulong queued=ingress.QueuedApplicationCount;bool faulted=ingress.IsFaulted;int messages=pump!.QueuedMessageCount;
        var state=runTransport!.State;ingress.Dispose();ingress.Dispose();
        Check(pump.QueuedMessageCount==messages&&runTransport.State==state,"Ingress dispose preserves borrowed pump and transport");
        GD.Print("soe_application_ingress_cleanup="+JsonSerializer.Serialize(new{Disposed=true,IsFaulted=faulted,ApplicationsQueued=queued,ControlsTransferred=soeIngressControls,
            BorrowedPumpPreserved=true,BorrowedTransportPreserved=true,Sent=false}));
    }
}
