
import * as dgram from 'dgram';

enum DiscoveryMessage {
  FindServer = 100,
  AnnounceServer = 101,
}

export const DISCOVERY_ADDRESS = '228.255.255.255';
export const DISCOVERY_PORT = 50962;

interface PeerDiscoveryCallback {
  onStarted: () => void;
  onFindServer: (address: string) => void;
  onWarn: (warning: string) => void;
  onError: (error: Error) => void;
}

export class PeerDiscoveryServer {
  static #instance: PeerDiscoveryServer | null = null;

  #socket: dgram.Socket;
  #callback: PeerDiscoveryCallback;


  private constructor(callback: PeerDiscoveryCallback) {
    this.#callback = callback;
    this.createServer();
  }

  private createServer() {
    const socket = dgram.createSocket({ type: 'udp4', reuseAddr: true});
    socket.on('message', (msg, rInfo) => { this.handleMessage(msg, rInfo); });
    socket.on('error', (err) => {
      this.handleError(err);
    });
    socket.on('close', () => {
      console.log("PeerDiscoveryServer is closed");
    });
    socket.bind(DISCOVERY_PORT, () => {
      socket.addMembership(DISCOVERY_ADDRESS);
      console.log(`PeerDiscoveryServer listening on ${DISCOVERY_ADDRESS}:${DISCOVERY_PORT}`);
    });
    this.#callback.onStarted();
    this.#socket = socket;
  }

  private handleMessage(msg: Buffer, rInfo: dgram.RemoteInfo) {
    if (msg.length !== 4) {
      const warnMsg = `PeerDiscoveryServer.handleMessage recieved garbage: ${msg.toString()}`;
      console.warn(warnMsg);
      this.#callback.onWarn(warnMsg);
      return;
    }
    if (msg.readInt32LE(0) !== DiscoveryMessage.FindServer) {
      const warnMsg = `PeerDiscoveryServer.handleMessage found another server running on ${rInfo.address}`;
      console.warn(warnMsg);
      this.#callback.onWarn(warnMsg);
      return;
    }
    console.log(`PeerDiscoveryServer.handleMessage received findServer from ${rInfo.address}`);
    this.#callback.onFindServer(rInfo.address);
    const response = Buffer.alloc(4);
    response.writeInt32LE(DiscoveryMessage.AnnounceServer, 0);
    this.#socket.send(response, DISCOVERY_PORT, DISCOVERY_ADDRESS, (error, bytes) => {
      if (error) {
        this.handleError(error);
      } else {
        console.log("PeerDiscoveryServer.handleMessage AnnounceServer sent successfully");
      }
    });
  }

  private handleError(err: Error) {
    console.error(`PeerDiscoveryServer.handleError received fatal error ${err}; resetting the server after 3s`);
    this.#callback.onError(err);
    this.#socket.close();
    setTimeout(() => { this.createServer() }, 3000);
  }

  // Static method to start or update the server
  public static StartServer(newCallback: PeerDiscoveryCallback) {
    const serverInstance = PeerDiscoveryServer.#instance;
    if (serverInstance) {
      console.log('PeerDiscoveryServer.startUdpServer(callback) already running, updating callback.');
      serverInstance.#callback = newCallback;
    } else {
      console.log('PeerDiscoveryServer.startUdpServer(callback) creating new server instance');
      PeerDiscoveryServer.#instance = new PeerDiscoveryServer(newCallback);
    }
  }

  public static StopServer() {
    const serverInstance = PeerDiscoveryServer.#instance;
    if (serverInstance) {
      serverInstance.#socket.close();
      PeerDiscoveryServer.#instance = null;
    }
  }
}