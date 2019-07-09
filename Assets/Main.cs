// Author(s): (1) Immersha Entertainment, (2)Jeremy Deats
// Copyright 2019
//
// License:
// By using this source code and any and all derivied products that may come from this
// soruce code, you agree to the following terms-
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using Nakama.Ninja;
using Nakama.Snippets;
using Nakama.TinyJson;
using Assets;
using System;

public class Main : MonoBehaviour
{
    // for this proof of concept we're going to use a few static deviceIDs
    // in a real project we would pull the deviceid using 
    // https://docs.unity3d.com/ScriptReference/SystemInfo-deviceUniqueIdentifier.html
    // or we would pick a different Nakama authorization method (e.g. email/userid)
    string deviceID1 = "3ba9c6af-56a7-46a8-af53-f33d485948d";
    string deviceID2 = "fbb3e225-66a7-86a3-fc43-b55d485949e";

    // private variables
    int opCode = 1;
    bool multiplayerStarted = false;
    List<IUserPresence> opponentList;
    string userID = "";
    string userName = "";

    // Nakama objects and interfaces
    Client client = null;
    ISocket socket = null;
    IMatch match = null;
    ISession session = null;

    // public variables
    public GameObject Player1;
    public GameObject Player2;
    public bool CreateMatch = false;
    public bool IsPlayer1 = true;
    public string MatchID = "";
    public Payload userPayload;
    public Payload secondPlayerPayload;

   // float z = (float)-6.12;


    // Start is called before the first frame update
    void Start()
    {
        // create an instance of our objec to be serialized and sent with each packet.
        // we do this on start, because it would be resource expensive to reinit this object 
        // each time we want to send a packet. 
        userPayload = new Payload();

        // create an instance of the Nakama Client
        // replace 127.0.0.1 with the IP of your Nakama server. 
        client = new Client("http", "127.0.0.1", 7350, "defaultkey");
     
        // open a new socket
        socket = client.NewSocket();

        // call Nakama Authorization with our deviceid
        DoAuth();

    }

    async void StartMatch()
    {
       
        // We'll either create a new match or join an existing one.
        if (CreateMatch)
        {
            match = await socket.CreateMatchAsync();
            Debug.Log("match id:" + match.Id);
            MatchID = match.Id;
        }
        else
        {
            match = await socket.JoinMatchAsync(MatchID);
        }
       
        // handle when opponents in match send messages
        socket.ReceivedMatchState += Socket_ReceivedMatchState;

        opponentList = new List<IUserPresence>();
    }

 
    // each time another player in the match sends a packet it will be recieved by this
    // handler
    private void Socket_ReceivedMatchState(IMatchState obj)
    {
        // ignore our own messages
        if (obj.UserPresence.UserId != userID)
        {
            // remember "second player" is relative. it will always represent the other player.
            // in this proof of concept we only have one additional player allowed. If you had 
            // more than one, you would need to determine the userID and possibly add some
            // conditional logic based on that to deserialize and adjust your local GameObject
            // transforms.
            secondPlayerPayload = (Payload)Utility.ByteArrayToObject(obj.State);
        }

    }


    async void DoAuth()
    {
        string device_id = "";

        if (IsPlayer1)
        {
            device_id = deviceID1;
        } else
        {
            device_id = deviceID2;
        }
        
        // call Nakama authenticate service
        session = await client.AuthenticateDeviceAsync(device_id);

        // extract userID and userName from the returned session
        userID = session.UserId;
        userName = session.UserId;

        // connect to the session with our socket
        await socket.ConnectAsync(session);

        // start the match
        StartMatch();
        multiplayerStarted = true;

    }

    void BuildSendPayload()
    {
        if (multiplayerStarted)
        {
            // store our players local transform in the userPayload object.
            // if this were to move beyond proof of concept, we would want to expand the 
            // Payload class to include additional properties and methods to share our state
            // with the other players.
            // Keep in mind the more data we stuff into the Payload object the large the serialized
            // byte array will be. That will translate into higher bandwidth requirements
            userPayload.x = Player1.transform.localPosition.x;
            userPayload.y = Player1.transform.localPosition.y;
            userPayload.z = Player1.transform.localPosition.z;

            // serialize the object to a byte array
            byte[] pl = Utility.ObjectToByteArray(userPayload);

            // call Nakama SendMatchState async end-point passing in the byte array.
            socket.SendMatchStateAsync(match.Id, 1, pl);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // read keyboard input from arrow keys and increment/decrement transforms
        if (Input.GetKey(KeyCode.UpArrow))
        {
            Player1.transform.localPosition = new Vector3(
                Player1.transform.localPosition.x,
                Player1.transform.localPosition.y,
                Player1.transform.localPosition.z - (float)0.1
                 );
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            Player1.transform.localPosition = new Vector3(
              Player1.transform.localPosition.x,
              Player1.transform.localPosition.y,
              Player1.transform.localPosition.z + (float)0.1
               );
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            Player1.transform.localPosition = new Vector3(
                Player1.transform.localPosition.x + (float)0.1,
                Player1.transform.localPosition.y,
                Player1.transform.localPosition.z
                 );
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            Player1.transform.localPosition = new Vector3(
              Player1.transform.localPosition.x - (float)0.1,
              Player1.transform.localPosition.y,
              Player1.transform.localPosition.z
               );
        }

        // use the data provided in our secondPlayerLoad Payload instance to adjust the 
        // local transform for the other players cube.
        Player2.transform.localPosition = new Vector3(
             secondPlayerPayload.x,
             secondPlayerPayload.y,
             secondPlayerPayload.z);
 
        // send our serialized payload to other clients
        BuildSendPayload();
   
    }
}
