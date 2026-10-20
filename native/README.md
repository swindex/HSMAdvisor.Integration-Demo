# Native client (C / C++)

`HSMAdvisorIntegration.dll` is the native counterpart of `HSMAdvisor.Integration.dll`, for hosts
that cannot load .NET (for example a Fusion 360 C++ add-in). It finds and starts HSMAdvisor, checks
its version and exchanges JSON messages over the same pipe; it does not use .NET.

```
native/
├── include/
│   ├── hsmadvisor_integration.h          # the C API
│   └── hsmadvisor_integration_loader.h   # header-only loader (compile into your host)
└── sample/                               # console sample: Hello + Calculate
lib/native/
├── x64/HSMAdvisorIntegration.dll         # copies you may ship with your integration
└── x86/HSMAdvisorIntegration.dll
```

## Loading

HSMAdvisor installs the DLL as `<HSMAdvisor folder>\integration\<x64|x86>\HSMAdvisorIntegration.dll`.
The loader prefers that copy, which is updated together with HSMAdvisor, and falls back to a copy you ship.
The fallback is what makes your integration work with HSMAdvisor 3.2.0, which does not install the DLL.

```cpp
#include "hsmadvisor_integration_loader.h"

hsma_api api;
if (hsma_load(&api, L"C:\\path\\to\\your\\plugin\\HSMAdvisorIntegration.dll") != HSMA_LOAD_OK)
    return; // HSMA_LOAD_NOT_FOUND / HSMA_LOAD_TOO_OLD

hsma_client* client = api.client_create("My CAM", "1.0");   // keep one for the app's lifetime
api.client_set_minimum_version(client, "3.2.0");

char* payload = NULL; char* error = NULL;
int status = api.request(client, "EditCalculation", request_json, (long long)myHwnd, cancel, &payload, &error);
// HSMA_OK: payload = {"calculation":{...}}   HSMA_CANCELLED: user cancelled
// HSMA_ERROR / HSMA_BUSY: error = {"code":"...","message":"..."}
api.free_memory(payload);
api.free_memory(error);
```

* `hsma_request` blocks. Call it from a worker thread when your UI must stay responsive.
* To close an open HSMAdvisor dialog from code, pass a token from `cancel_create` and call
  `cancel_signal` from any thread.
* Define `HSMA_LOADER_VERIFY_SIGNATURE` before including the loader to accept only signed DLLs.

## Payloads

Payloads are the JSON messages from the protocol (see the main README's message table). Record
fields are **strings**, for example `"diameter":"12"`, and booleans are `"1"`/`"0"`. Fields you leave
out take HSMAdvisor's defaults. Each value's unit is set by its `_mm` flag (`"diameter_mm":"1"`).
For metric data, send the cut's `diameter` and `stickout` with their `_mm` flags as well, because
those win over the tool's. Set `feed_mm`/`result_units_mm` to get the results in metric.

## Sample

```
msbuild native\sample\NativeSample.vcxproj /p:Configuration=Release /p:Platform=x64
native\sample\bin\x64\Release\HSMAdvisor.NativeSample.exe
```
