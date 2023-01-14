### INetBehaviour

Implement `INetBehavior` interface and add Net.Foundation package. Callback `OnNetworkAuthorityChanged` will be called automatically.

Becoming server for the first time doesn't trigger this, but becoming for the first time client - does, so default behaviour should be servers.

```csharp
void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
{
    switch (authority)
    {
        case NetworkAuthority.Client:
            IAmServer = false;
            //Do stuff here for disabling behaviour like unregister from fixed update, stop coroutines, stop game systems, etc.
            //Rigidbodies are automatically handled and will become kinematic
            Debug.Log($"{gameObject.name} is now Client");
            break;
        case NetworkAuthority.Server:
            IAmServer = true;
            //This is not called on initial run. It will be called just after authority changed, but not on initial server authority.
            //Do stuff here for reregistering for fixed update and stuff.
            Debug.Log($"{gameObject.name} is now Server");
            break;
    }
}
```

### INetStreamer

Net streamer automatically syncs values and you don't need to provide gameObject ID or anything. But you need to handle data transfer manually.

Order in which stuff is written matters. Read at the same order.

It is recomended that all values which changes often (e.g.: each frame) especially if they are used only for updating visuals to be quantized. Quantization amount should be picked manualy depending on the minimum/maximum values, and precision needed

```csharp
float floatValueToSync;

const float MAX_FLOAT_VALUE = 100;
const int BITS = 8;

//Writes info to send 
void INetStreamer.CollectState(IStreamWriter stream)
{
    //it is best to quntize. That way it will be lest offten that we need to send data.
    //More bits - bigger precission
    var val = floatValueToSync.Quantize(MAX_FLOAT_VALUE, BITS);
    stream.Write(val, BITS);
}

//Reads info that was received
void INetStreamer.ApplyState(IStreamReader state)
{
    floatValueToSync = state.ReadInt32(BITS).Dequantize(MAX_FLOAT_VALUE, BITS);
}

void INetStreamer.ApplyLerpedState(IStreamReader state0, IStreamReader state1, float mix, float timeBetweenFrames)
{
    var val0 = state0.ReadInt32(BITS).Dequantize(MAX_FLOAT_VALUE, BITS);
    var val1 = state1.ReadInt32(BITS).Dequantize(MAX_FLOAT_VALUE, BITS);
    floatValueToSync = Mathf.Lerp(val0, val1, mix);
}

//Write just partial amount of data in order to reduce the amount we send
void INetStreamer.CalculateDelta(IStreamReader state0, IStreamReader state1, IStreamWriter delta)
{
    var q0 = state0 == null ? 0 : state0.ReadInt32(BITS);
    var q1 = state1.ReadInt32(BITS);
    if (q0 == q1) //if both values same - then nothing to send
    {
        delta.Write(false);//one bit to indicate that nothing needs to change
    }
    else
    {
        delta.Write(true);//indicate that there are some changes
        delta.Write(q1, BITS);//and write those changes
    }
}

//Receives info from server written in calculateDelta
void INetStreamer.AddDelta(IStreamReader state0, IStreamReader delta, IStreamWriter result)
{
    var q0 = state0 == null ? 0 : state0.ReadInt32(BITS);
    if (!delta.ReadBool())
    {
        result.Write(q0, BITS); // not changed
    }
    else
    {
        //changed. We write absolute, because we don't really gain a lot by writing relative, with BITS=4
        //Make sure, if you increase bits to consider writing relative here
        //TODO: need example of how that could be done
        int q1 = delta.ReadInt32(BITS);
        result.Write(q1, BITS);
    }
}
```

### Samples

There are 4 samples in NetProto scene. (TODO: move after merging new networking)

* CustomEventsSample
* NetStreamerSample
* OnClientJoinedSample
* OnNetworkAuthorityChangedSample