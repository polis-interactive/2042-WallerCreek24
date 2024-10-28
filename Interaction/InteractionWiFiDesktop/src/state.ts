import { ActorRefFrom, AnyEventObject, assign, createMachine, EventFrom, fromCallback, sendTo, setup, SnapshotFrom } from "xstate";
import { PeerDiscoveryServer } from "./peer-discovery";
import { MeasurementRequest, MeasurementResponse, MeasurementServer } from "./measurement";

const PeerDiscoveryLogic = fromCallback(({ sendBack }) => {
  PeerDiscoveryServer.StartServer({
    onStarted: () => {
      sendBack({
        type: 'PEER_DISCOVERY_STARTED'
      });
    },
    onFindServer: (address: string) => {
      sendBack({
        type: 'PEER_DISCOVERY_DISCOVERED',
        address
      });
    },
    onWarn: (warning: string) => {
      sendBack({
        type: 'PEER_DISCOVERY_WARNING',
        warning
      });
    },
    onError: (error: Error) => {
      sendBack({
        type: 'PEER_DISCOVERY_ERROR',
        error: error.message
      })
    }
  })

  return () => {
    PeerDiscoveryServer.StopServer();
  }
});

export const MeasurementLogic = fromCallback(({ sendBack, receive}) => {
  const measurementServer = MeasurementServer.StartServer({
    onStarted: () => {
      sendBack({
        type: 'MEASUREMENT_STARTED',    
      })
    },
    onStopped: () => {
      sendBack({
        type: 'MEASUREMENT_STOPPED',    
      })
    },
    onConnected: (address: string) => {
      sendBack({
        type: 'MEASUREMENT_CONNECTED',
        address: address  
      });
    },
    onDisconnected: () => {
      sendBack({
        type: 'MEASUREMENT_DISCONNECTED',
      });
    },
    onMeasurement: (response: MeasurementResponse) => {
      sendBack({
        type: 'MEASUREMENT_RESPONSE',
        response
      });
    },
    onError: () => {
      sendBack({
        type: 'MEASUREMENT_ERROR'
      });
    }
  });
  receive((event: AnyEventObject) => {
    measurementServer.SendMeasurementRequest(event.request as MeasurementRequest);
  });
  return () => {
    MeasurementServer.StopServer();
  }
})

export type InteractionWiFiMachineContext = {
  address: string | null,
  responses: Array<MeasurementResponse>
};


export const InteractionWiFiMachine = setup({
  types: {
    context: {} as InteractionWiFiMachineContext
  },
  actors: {
    peerDiscoveryActor: PeerDiscoveryLogic,
    measurementActor: MeasurementLogic
  },
  actions: {
    setConnectionAddress: assign({
      address: ({ event }) => event.address
    }),
    resetConnectionAddress: assign({
      address: () => null
    }),
    proxyMeasurementRequest: sendTo('measurementActor', ({event}) => event),
    addMeasurement: assign({
      responses: ({ event, context }) => [...context.responses, event.response]
    })
  }
}).createMachine({
  id: 'interactionWiFiDesktopMachine',
  context: {
    address: null,
    responses: []
  },
  invoke: [
    {
      id: 'peerDiscoveryActor',
      src: 'peerDiscoveryActor'
    },
    {
      id: 'measurementActor',
      src: 'measurementActor'
    }
  ],
  initial: 'waitingForServer',
  states: {
    waitingForServer: {},
    serverRunning: {
      initial: 'waitingForConnection',
      states: {
        waitingForConnection: {},
        connected: {
          exit: 'resetConnectionAddress',
          initial: 'idle',
          states: {
            idle: {
              on: {
                MEASUREMENT_REQUEST: {
                  actions: 'proxyMeasurementRequest',
                  target: 'waitingForResponse'
                }
              }
            },
            waitingForResponse: {
              on: {
                MEASUREMENT_RESPONSE: {
                  target: 'idle',
                  actions: 'addMeasurement'
                },
                MEASUREMENT_ERROR: {
                  target: 'idle'
                }
              }
            }
          }
        }
      },
      on: {
        MEASUREMENT_CONNECTED: {
          target: '.connected',
          actions: 'setConnectionAddress'
        },
        MEASUREMENT_DISCONNECTED: {
          target: '.waitingForConnection'
        }
      }
    },
  },
  on: {
    MEASUREMENT_STARTED: {
      target: '.serverRunning'
    },
    MEASUREMENT_STOPPED: {
      target: '.waitingForServer'
    }
  }
})

export type InteractionSnapshot = SnapshotFrom<typeof InteractionWiFiMachine>;
export type InteractionSnapshotCallback = (snapshot: InteractionSnapshot) => void;

export type InteractionEvent = EventFrom<typeof InteractionWiFiMachine>;
export type InteractionEventCallback = (event: InteractionEvent) => void;
