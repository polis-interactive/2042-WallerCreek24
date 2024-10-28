
import * as net from 'net';
import stringify from 'safe-stable-stringify';

const MEASUREMENT_PORT = 3425;
const MEASUREMENT_DELIMETER = '\n';

const IsObject = (x: unknown): x is Object => {
  return typeof x === 'object' && x !== null && !Array.isArray(x);
}

export type MeasurementRequest = {
  note: string,
  samples: number,
  timeout: number
};

const IsMeasurementRequest = (x: unknown): x is Measurement => {
  if (!IsObject(x)) {
    return false;
  }
  return 'samples' in x && typeof x.samples === 'number' &&
    'timeout' in x && typeof x.timeout === 'number' &&
    'note' in x && typeof x.note === 'string';
}

export type Measurement = {
    ftm: number,
    rtt: number
};

const IsMeasurement = (x: unknown): x is Measurement => {
    if (!IsObject(x)) {
      return false;
    }
    return 'ftm' in x && typeof x.ftm === 'number' &&
        'rtt' in x && typeof x.rtt === 'number';
}

export type MeasurementReply = Array<Measurement>;

const IsMeasurementReply = (x: unknown): x is MeasurementReply => {
    if (!Array.isArray(x)) {
        return false;
    }
    return x.every(IsMeasurement)
}

export type MeasurementResponse = {
    request: MeasurementRequest;
    reply: MeasurementReply;
}

interface MeasurementCallback {
    onStarted: () => void;
    onStopped: () => void;
    onConnected: (address: string) => void;
    onDisconnected: () => void;
    onMeasurement: (resp: MeasurementResponse) => void;
    onError: () => void;
  }

export class MeasurementServer {
    static #instance: MeasurementServer | null = null;

    #server: net.Server;
    #socket: net.Socket | null = null;
    #request: MeasurementRequest | null = null;
    #buffer: string = '';
    #callback: MeasurementCallback;

    private constructor(callback: MeasurementCallback) {
        this.#callback = callback;
        this.createServer();
    }

    private createServer() {
        const server = net.createServer();
        server.on('connection', (socket) => { this.handleConnection(socket); });
        server.on('error', (err) => { this.handleServerError(err); });
        server.on('close', () => { 
          console.log("MeasurementServer is closed")
          this.#callback.onStopped();
        });
        server.listen(MEASUREMENT_PORT, () => {
          console.log(`MeasurementServer listening on port ${MEASUREMENT_PORT}`);
          this.#callback.onStarted();
        });
        this.#server = server;
    }

    private handleServerError(err: Error) {
        console.error(`MeasurementServer.handleError received error: ${err.message}`);
        if (this.#request) {
            this.#request = null;
            this.#callback.onError();
        }
        if (this.#socket) {
            this.#socket.destroy();
            this.#socket = null;
            this.#callback.onDisconnected();
        }
        this.#server.close();
        setTimeout(() => this.createServer(), 3000);
    }

    private handleConnection(socket: net.Socket) {
        const clientAddress = `${socket.remoteAddress}:${socket.remotePort}`;
        if (this.#request) {
            console.log("MeasurementServer.handleConnection: couldn't handle request");
            this.#request = null;
            this.#callback.onError();
        }
        if (this.#socket) {
            const previousAddress = `${this.#socket.remoteAddress}:${this.#socket.remotePort}`;
            console.log('MesurementServer.handleConnection closing existing connection to ' + previousAddress);
            this.#socket.destroy();
        }
        this.#socket = socket;
        console.log(`MeasurementServer.handleConnection connected to ${clientAddress}`);
        this.#callback.onConnected(clientAddress);

        this.#buffer = '';

        socket.on('data', (data) => { this.handleMessage(data); });
        socket.on('end', () => { this.handleDisconnection(clientAddress); });
        socket.on('error', (err) => { this.handleSocketError(err); });
    }

    public SendMeasurementRequest(req: MeasurementRequest) {
        if (!this.#socket) {
            // you must be confused
            console.warn('MeasurementServer.sendMeasurementRequest no client connected');
            this.#callback.onDisconnected();
            return;
        } else if (this.#request) {
            this.handleSocketError(new Error('request already in flight'));
            return;
        } else if (!IsMeasurementRequest(req)) {
          this.handleSocketError(new Error("no I know you didn't mess up that bad"));
          return;
        }
        const requestString = stringify(req) + MEASUREMENT_DELIMETER;
        const requestBuffer = Buffer.from(requestString);
        this.#socket.write(requestBuffer, (err) => {
            if (err) {
                this.handleSocketError(err);
            } else {
                console.log(`MeasurementServer.sendMeasurementRequest sent request: ${JSON.stringify(req)}`); 
                this.#request = req;
            }
        })
    }

    private handleMessage(data: Buffer) {
        if (!this.#request) {
            this.handleSocketError(new Error("No request to answer has been started..."));
            return;
        }
        this.#buffer += data.toString();
        let newLineIndex: number;
        while ((newLineIndex = this.#buffer.indexOf(MEASUREMENT_DELIMETER)) !== -1) {
            const message = this.#buffer.slice(0, newLineIndex);
            this.#buffer = this.#buffer.slice(newLineIndex + 1); 
            try {
                const reply = JSON.parse(message);
                if (!IsMeasurementReply(reply)) {
                    throw new Error(`Recieved garbage response ${reply}`);
                }
                this.#callback.onMeasurement({
                    request: this.#request,
                    reply
                });
                this.#request = null;
            } catch (err) {
                this.handleSocketError(err);
            }
        }
    }

    private handleDisconnection(address: string) {
        if (this.#request) {
            this.#request = null;
            this.#callback.onError();
        }
        console.log(`MeasurementServer.handleDisconnection disconnected from ${address}`);
        if (this.#socket) {
            this.#callback.onDisconnected();
            this.#socket = null;
        }
    }

    private handleSocketError(err: Error) {
        console.error(`MeasurementServer.handleSocketError ${err.message}`);
        if (this.#request) {
            this.#request = null;
            this.#callback.onError();
        }
        this.#socket.destroy();
        this.#socket = null;
        this.#callback.onDisconnected();
    }

    public static StartServer(newCallback: MeasurementCallback): MeasurementServer {
        const serverInstance = MeasurementServer.#instance;
        if (serverInstance) {
            console.log('MeasurementServer.startServer(callback) already running, updating callback.');
            serverInstance.#callback = newCallback;
        } else {
            console.log('MeasurementServer.startServer(callback) creating new server instance');
            MeasurementServer.#instance = new MeasurementServer(newCallback);
        }
        return MeasurementServer.#instance;
    }

    public static StopServer() {
        const serverInstance = MeasurementServer.#instance;
        if (serverInstance) {
          serverInstance.#server.close();
          MeasurementServer.#instance = null;
        }
    }

}