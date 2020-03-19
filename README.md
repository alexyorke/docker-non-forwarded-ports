# docker-non-forwarded-ports
Warns when forwarding an internal Docker container port that doesn't have a listener inside of the container.

Say you start the container `docker run -it --rm -p 8000:80 --name aspnetcore_sample mcr.microsoft.com/dotnet/core/samples:aspnetapp` but make a mistake and instead forward port `81` instead of `80`. Docker won't say anything, the server won't say anything, but the server will not be reachable at port `8000` because the server is configured to use port `80`. If `docker-non-forward-port` finds that you tried to forward an internal port, and nobody is using that port inside of the container, then `docker-non-forwarded-port` will beep three times and let you know that a port isn't being forwarded correctly with the container id.

This application runs in the background and checks all newly started containers. The containers must have `/bin/bash` installed for this to work.

Sample output:

```
start->de4cfa42f9f50edb8fe5c82734d3a9f1a490818cdadd2590e1b227057ae0c5a6
Warning: container de4cfa42f9f50edb8fe5c82734d3a9f1a490818cdadd2590e1b227057ae0c5a6 does not have any forwarded ports.
start->62a3b482eec86b344199862dd7c3fe0eadee49e4eb6b2c6512238993ba9224d7
start->7545ad5f4bcf93ba01ffb1085527affd83888a29f2492980e2eede15b7e49af1
Warning: container 7545ad5f4bcf93ba01ffb1085527affd83888a29f2492980e2eede15b7e49af1 does not have any forwarded ports.
```
