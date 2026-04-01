import mitt from 'mitt'

type Events = {
  'workspace-updated': void;
};

export default defineNuxtPlugin(() => {
  const emitter = mitt<Events>()
  return {
    provide: {
      bus: emitter
    }
  }
})