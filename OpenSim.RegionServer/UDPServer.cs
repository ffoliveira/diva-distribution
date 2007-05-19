using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.world;
using OpenSim.Terrain;
using OpenSim.Framework.Interfaces;
using OpenSim.Framework.Types;
using OpenSim.UserServer;
using OpenSim.Assets;
using OpenSim.CAPS;
using OpenSim.Framework.Console;
using Nwc.XmlRpc;
using OpenSim.Servers;
using OpenSim.GenericConfig;

namespace OpenSim
{
    public delegate AuthenticateResponse AuthenticateSessionHandler(LLUUID sessionID, LLUUID agentID, uint circuitCode);

    public class UDPServer : OpenSimNetworkHandler
    {
        protected Dictionary<EndPoint, uint> clientCircuits = new Dictionary<EndPoint, uint>();
        public Socket Server;
        protected IPEndPoint ServerIncoming;
        protected byte[] RecvBuffer = new byte[4096];
        protected byte[] ZeroBuffer = new byte[8192];
        protected IPEndPoint ipeSender;
        protected EndPoint epSender;
        protected AsyncCallback ReceivedData;
        protected PacketServer _packetServer;

        protected int listenPort;
        protected Grid m_gridServers;
        protected World m_localWorld;
        protected AssetCache m_assetCache;
        protected InventoryCache m_inventoryCache;
        protected RegionInfo m_regionData;
        protected bool m_sandbox = false;
        protected bool user_accounts = false;
        protected ConsoleBase m_console;
        protected AuthenticateSessionsBase m_authenticateSessionsClass;

        public AuthenticateSessionHandler AuthenticateHandler;

        public PacketServer PacketServer
        {
            get
            {
                return _packetServer;
            }
            set
            {
                _packetServer = value;
            }
        }

        public World LocalWorld
        {
            set
            {
                this.m_localWorld = value;
                this._packetServer.LocalWorld = this.m_localWorld;
            }
        }

        public UDPServer(int port, Grid gridServers, AssetCache assetCache, InventoryCache inventoryCache, RegionInfo _regionData, bool sandbox, bool accounts, ConsoleBase console , AuthenticateSessionsBase authenticateClass)
        {
            listenPort = port;
            this.m_gridServers = gridServers;
            this.m_assetCache = assetCache;
            this.m_inventoryCache = inventoryCache;
            this.m_regionData = _regionData;
            this.m_sandbox = sandbox;
            this.user_accounts = accounts;
            this.m_console = console;
            this.m_authenticateSessionsClass = authenticateClass;
            this.CreatePacketServer();
            
            //set up delegate for authenticate sessions
            this.AuthenticateHandler = new AuthenticateSessionHandler(this.m_authenticateSessionsClass.AuthenticateSession);
        }

        protected virtual void CreatePacketServer()
        {
            PacketServer packetServer = new PacketServer(this);
        }

        protected virtual void OnReceivedData(IAsyncResult result)
        {
            ipeSender = new IPEndPoint(IPAddress.Any, 0);
            epSender = (EndPoint)ipeSender;
            Packet packet = null;
            int numBytes = Server.EndReceiveFrom(result, ref epSender);
            int packetEnd = numBytes - 1;

            packet = Packet.BuildPacket(RecvBuffer, ref packetEnd, ZeroBuffer);

            // do we already have a circuit for this endpoint
            if (this.clientCircuits.ContainsKey(epSender))
            {
                //if so then send packet to the packetserver
                this._packetServer.ClientInPacket(this.clientCircuits[epSender], packet);
            }
            else if (packet.Type == PacketType.UseCircuitCode)
            {
                // new client
                this.AddNewClient(packet);
            }
            else
            { // invalid client
                Console.Error.WriteLine("UDPServer.cs:OnReceivedData() - WARNING: Got a packet from an invalid client - " + epSender.ToString());
            }

            Server.BeginReceiveFrom(RecvBuffer, 0, RecvBuffer.Length, SocketFlags.None, ref epSender, ReceivedData, null);
        }

        protected virtual void AddNewClient(Packet packet)
        {
            UseCircuitCodePacket useCircuit = (UseCircuitCodePacket)packet;
            this.clientCircuits.Add(epSender, useCircuit.CircuitCode.Code);
            bool isChildAgent = false;

            SimClient newuser = new SimClient(epSender, useCircuit, m_localWorld, _packetServer.ClientThreads, m_assetCache, m_gridServers.GridServer, this, m_inventoryCache, m_sandbox, isChildAgent, this.m_regionData, m_authenticateSessionsClass);
            if ((this.m_gridServers.UserServer != null) && (user_accounts))
            {
                newuser.UserServer = this.m_gridServers.UserServer;
            }
            //OpenSimRoot.Instance.ClientThreads.Add(epSender, newuser);
            this._packetServer.ClientThreads.Add(useCircuit.CircuitCode.Code, newuser);
        }

        public void ServerListener()
        {
            m_console.WriteLine("UDPServer.cs:ServerListener() - Opening UDP socket on " + listenPort);

            ServerIncoming = new IPEndPoint(IPAddress.Any, listenPort);
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Server.Bind(ServerIncoming);

            m_console.WriteLine("UDPServer.cs:ServerListener() - UDP socket bound, getting ready to listen");

            ipeSender = new IPEndPoint(IPAddress.Any, 0);
            epSender = (EndPoint)ipeSender;
            ReceivedData = new AsyncCallback(this.OnReceivedData);
            Server.BeginReceiveFrom(RecvBuffer, 0, RecvBuffer.Length, SocketFlags.None, ref epSender, ReceivedData, null);

            m_console.WriteLine("UDPServer.cs:ServerListener() - Listening...");

        }

        public virtual void RegisterPacketServer(PacketServer server)
        {
            this._packetServer = server;
        }

        public virtual void SendPacketTo(byte[] buffer, int size, SocketFlags flags, uint circuitcode)//EndPoint packetSender)
        {
            // find the endpoint for this circuit
            EndPoint sendto = null;
            foreach (KeyValuePair<EndPoint, uint> p in this.clientCircuits)
            {
                if (p.Value == circuitcode)
                {
                    sendto = p.Key;
                    break;
                }
            }
            if (sendto != null)
            {
                //we found the endpoint so send the packet to it
                this.Server.SendTo(buffer, size, flags, sendto);
            }
        }

        public virtual void RemoveClientCircuit(uint circuitcode)
        {
            foreach (KeyValuePair<EndPoint, uint> p in this.clientCircuits)
            {
                if (p.Value == circuitcode)
                {
                    this.clientCircuits.Remove(p.Key);
                    break;
                }
            }
        }

        public virtual AuthenticateResponse AuthenticateSession(LLUUID sessionID, LLUUID agentID, uint circuitCode)
        {
           return this.AuthenticateHandler(sessionID, agentID, circuitCode);
        }
    }
}