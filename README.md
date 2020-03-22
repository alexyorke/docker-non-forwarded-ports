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

## Roadmap

This app uses an invasive approach and runs the commands on all newly-instantiated Docker containers. This could cause side-effects, and also requires that bash is installed.

- Remove invasive check and replace with stateless check. The running Docker container can be streamed as a tar file, and so the filesystem can be accessed through this file. When the required files are found, then the stream should stop reading as the other files are not required. It is not possible to access the required files via mounting the container's filesystem as these directories are not accessible.
- Check the running containers periodically. When a container is started, it may (throughout its lifetime) stop broadcasting to that port. This script should run on already running containers periodically, or have the option to do so.
- Use the correct streaming API for getting Docker events. Currently, it is sloppily hacked together as I could not get the stream to broadcast JSON events. Since the stream doesn't have any linebreaks, it has to parse the JSON char-by-char, and stops when the string is valid JSON. This is slow, inefficient, and unsafe.
