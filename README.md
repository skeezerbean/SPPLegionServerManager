# -- Skeez's SPP Legion Server Manager --

This will start/stop servers as needed and show their status. This will require admin permission to be able to check processes that this launcher doesn't own (such as restarting while servers already running)

** **NOTE** ** -- There is no console access with this to the world server, no way to see status, and no way to send it commands. Check server.log if you need to see what's happening

Window size/position, path, and toggle switches will save on closing the app

Activating the Auto Start will start servers on launch

Activating the Auto Restart will start servers at any time if not running

** **NOTE** ** -- Manually stopping any/all servers will disable the Auto Restart

If you try starting BNet/World without the DB launched, it will launch the DB first. Until DB is running then it won't attempt to start BNet/World

If you stop the DB while BNet/World are running, it will shut those down first. It will not attempt to stop the DB until they are closed down

It will only report servers that are located in the Repack Path, and only if the path is NOT the default C:\

The servers will be shut down with a control+c signal sent to them internally in the code, so they will shut down safely rather than just killing the process

If your server.log is getting too large then you'll need to shut down the world server and delete/archive it, then restart again
