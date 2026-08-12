mergeInto(LibraryManager.library, {
  $MiniCoreWebSocketInstances: {},

  $MiniCoreInvoke: function (callback, signature, args) {
    var dynCall = Module['dynCall_' + signature];
    if (dynCall) {
      dynCall.apply(null, [callback].concat(args));
      return;
    }

    getWasmTableEntry(callback).apply(null, args);
  },

  $MiniCoreInvokeString__deps: ['$MiniCoreInvoke'],
  $MiniCoreInvokeString: function (callback, signature, args, value) {
    var text = value || '';
    var length = lengthBytesUTF8(text) + 1;
    var pointer = _malloc(length);
    try {
      stringToUTF8(text, pointer, length);
      MiniCoreInvoke(callback, signature, args.concat([pointer]));
    } finally {
      _free(pointer);
    }
  },

  MiniCoreWebSocketConnect__deps: ['$MiniCoreWebSocketInstances', '$MiniCoreInvoke', '$MiniCoreInvokeString'],
  MiniCoreWebSocketConnect: function (id, urlPointer, maximumMessageSize, onOpen, onMessage, onError, onClose) {
    if (MiniCoreWebSocketInstances[id]) {
      return 0;
    }

    try {
      var socket = new WebSocket(UTF8ToString(urlPointer));
      socket.binaryType = 'arraybuffer';
      var entry = {
        socket: socket,
        maximumMessageSize: maximumMessageSize,
        onOpen: onOpen,
        onMessage: onMessage,
        onError: onError,
        onClose: onClose
      };
      MiniCoreWebSocketInstances[id] = entry;

      socket.onopen = function () {
        MiniCoreInvoke(entry.onOpen, 'vi', [id]);
      };

      socket.onmessage = function (event) {
        if (!(event.data instanceof ArrayBuffer)) {
          socket.close(1003, 'Binary messages only.');
          return;
        }

        var bytes = new Uint8Array(event.data);
        if (bytes.byteLength > entry.maximumMessageSize) {
          socket.close(1009, 'Message is too large.');
          return;
        }

        var pointer = _malloc(bytes.byteLength);
        try {
          HEAPU8.set(bytes, pointer);
          MiniCoreInvoke(entry.onMessage, 'viii', [id, pointer, bytes.byteLength]);
        } finally {
          _free(pointer);
        }
      };

      socket.onerror = function (event) {
        MiniCoreInvokeString(entry.onError, 'vii', [id], event && event.message ? event.message : 'Browser WebSocket error.');
      };

      socket.onclose = function (event) {
        MiniCoreInvokeString(entry.onClose, 'viii', [id, event.code || 1006], event.reason || '');
        delete MiniCoreWebSocketInstances[id];
      };

      return 1;
    } catch (error) {
      delete MiniCoreWebSocketInstances[id];
      return 0;
    }
  },

  MiniCoreWebSocketSend__deps: ['$MiniCoreWebSocketInstances'],
  MiniCoreWebSocketSend: function (id, dataPointer, offset, length, maximumBufferedBytes) {
    var entry = MiniCoreWebSocketInstances[id];
    if (!entry || entry.socket.readyState !== WebSocket.OPEN) {
      return 0;
    }

    if (entry.socket.bufferedAmount + length > maximumBufferedBytes) {
      return -2;
    }

    var message = HEAPU8.slice(dataPointer + offset, dataPointer + offset + length);
    entry.socket.send(message);
    return 1;
  },

  MiniCoreWebSocketGetState__deps: ['$MiniCoreWebSocketInstances'],
  MiniCoreWebSocketGetState: function (id) {
    var entry = MiniCoreWebSocketInstances[id];
    return entry ? entry.socket.readyState : 3;
  },

  MiniCoreWebSocketClose__deps: ['$MiniCoreWebSocketInstances'],
  MiniCoreWebSocketClose: function (id, code, reasonPointer) {
    var entry = MiniCoreWebSocketInstances[id];
    if (!entry) {
      return;
    }

    var state = entry.socket.readyState;
    if (state === WebSocket.CONNECTING || state === WebSocket.OPEN) {
      entry.socket.close(code, UTF8ToString(reasonPointer));
    }
  },

  MiniCoreWebSocketDestroy__deps: ['$MiniCoreWebSocketInstances'],
  MiniCoreWebSocketDestroy: function (id) {
    var entry = MiniCoreWebSocketInstances[id];
    if (!entry) {
      return;
    }

    entry.socket.onopen = null;
    entry.socket.onmessage = null;
    entry.socket.onerror = null;
    entry.socket.onclose = null;
    if (entry.socket.readyState === WebSocket.CONNECTING || entry.socket.readyState === WebSocket.OPEN) {
      entry.socket.close(1000, 'Disposed');
    }

    delete MiniCoreWebSocketInstances[id];
  }
});
