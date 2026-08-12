mergeInto(LibraryManager.library, {
  $MiniCoreStorageDatabasePromise: null,

  $MiniCoreStorageInvoke: function (callback, signature, args) {
    var dynCall = Module['dynCall_' + signature];
    if (dynCall) {
      dynCall.apply(null, [callback].concat(args));
      return;
    }

    getWasmTableEntry(callback).apply(null, args);
  },

  $MiniCoreStorageInvokeError__deps: ['$MiniCoreStorageInvoke'],
  $MiniCoreStorageInvokeError: function (callback, requestId, error) {
    var text = error && error.message ? error.message : String(error || 'IndexedDB operation failed.');
    var length = lengthBytesUTF8(text) + 1;
    var pointer = _malloc(length);
    try {
      stringToUTF8(text, pointer, length);
      MiniCoreStorageInvoke(callback, 'vii', [requestId, pointer]);
    } finally {
      _free(pointer);
    }
  },

  $MiniCoreOpenStorageDatabase__deps: ['$MiniCoreStorageDatabasePromise'],
  $MiniCoreOpenStorageDatabase: function () {
    if (MiniCoreStorageDatabasePromise) {
      return MiniCoreStorageDatabasePromise;
    }

    MiniCoreStorageDatabasePromise = new Promise(function (resolve, reject) {
      var request = indexedDB.open('MiniCore.Storage.v1', 1);
      request.onupgradeneeded = function () {
        var database = request.result;
        if (!database.objectStoreNames.contains('records')) {
          database.createObjectStore('records');
        }
      };
      request.onsuccess = function () {
        resolve(request.result);
      };
      request.onerror = function () {
        reject(request.error || new Error('Unable to open IndexedDB.'));
      };
    });
    return MiniCoreStorageDatabasePromise;
  },

  MiniCoreStorageRead__deps: ['$MiniCoreOpenStorageDatabase', '$MiniCoreStorageInvoke', '$MiniCoreStorageInvokeError'],
  MiniCoreStorageRead: function (requestId, keyPointer, completed, failed) {
    var key = UTF8ToString(keyPointer);
    MiniCoreOpenStorageDatabase().then(function (database) {
      var transaction = database.transaction('records', 'readonly');
      var request = transaction.objectStore('records').get(key);
      request.onsuccess = function () {
        if (typeof request.result === 'undefined') {
          MiniCoreStorageInvoke(completed, 'viiii', [requestId, 0, 0, 0]);
          return;
        }

        var bytes = request.result instanceof Uint8Array
          ? request.result
          : new Uint8Array(request.result);
        var pointer = bytes.byteLength > 0 ? _malloc(bytes.byteLength) : 0;
        try {
          if (bytes.byteLength > 0) {
            HEAPU8.set(bytes, pointer);
          }
          MiniCoreStorageInvoke(completed, 'viiii', [requestId, pointer, bytes.byteLength, 1]);
        } finally {
          if (pointer) {
            _free(pointer);
          }
        }
      };
      request.onerror = function () {
        MiniCoreStorageInvokeError(failed, requestId, request.error);
      };
    }).catch(function (error) {
      MiniCoreStorageInvokeError(failed, requestId, error);
    });
  },

  MiniCoreStorageWrite__deps: ['$MiniCoreOpenStorageDatabase', '$MiniCoreStorageInvoke', '$MiniCoreStorageInvokeError'],
  MiniCoreStorageWrite: function (requestId, keyPointer, bytesPointer, length, completed, failed) {
    var key = UTF8ToString(keyPointer);
    var bytes = HEAPU8.slice(bytesPointer, bytesPointer + length);
    MiniCoreOpenStorageDatabase().then(function (database) {
      var transaction = database.transaction('records', 'readwrite');
      transaction.oncomplete = function () {
        MiniCoreStorageInvoke(completed, 'vii', [requestId, 1]);
      };
      transaction.onerror = function () {
        MiniCoreStorageInvokeError(failed, requestId, transaction.error);
      };
      transaction.objectStore('records').put(bytes, key);
    }).catch(function (error) {
      MiniCoreStorageInvokeError(failed, requestId, error);
    });
  },

  MiniCoreStorageDelete__deps: ['$MiniCoreOpenStorageDatabase', '$MiniCoreStorageInvoke', '$MiniCoreStorageInvokeError'],
  MiniCoreStorageDelete: function (requestId, keyPointer, completed, failed) {
    var key = UTF8ToString(keyPointer);
    MiniCoreOpenStorageDatabase().then(function (database) {
      var transaction = database.transaction('records', 'readwrite');
      transaction.oncomplete = function () {
        MiniCoreStorageInvoke(completed, 'vii', [requestId, 1]);
      };
      transaction.onerror = function () {
        MiniCoreStorageInvokeError(failed, requestId, transaction.error);
      };
      transaction.objectStore('records').delete(key);
    }).catch(function (error) {
      MiniCoreStorageInvokeError(failed, requestId, error);
    });
  },

  MiniCoreStorageExists__deps: ['$MiniCoreOpenStorageDatabase', '$MiniCoreStorageInvoke', '$MiniCoreStorageInvokeError'],
  MiniCoreStorageExists: function (requestId, keyPointer, completed, failed) {
    var key = UTF8ToString(keyPointer);
    MiniCoreOpenStorageDatabase().then(function (database) {
      var transaction = database.transaction('records', 'readonly');
      var request = transaction.objectStore('records').getKey(key);
      request.onsuccess = function () {
        MiniCoreStorageInvoke(completed, 'vii', [requestId, typeof request.result === 'undefined' ? 0 : 1]);
      };
      request.onerror = function () {
        MiniCoreStorageInvokeError(failed, requestId, request.error);
      };
    }).catch(function (error) {
      MiniCoreStorageInvokeError(failed, requestId, error);
    });
  }
});
