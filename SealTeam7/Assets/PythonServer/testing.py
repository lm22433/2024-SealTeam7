import win32event

READY_EVENT_NAME = "SealTeam7ColourImageReady"
DONE_EVENT_NAME = "SealTeam7HandLandmarksDone"

ready_event = win32event.CreateEvent(None, 0, 0, READY_EVENT_NAME)
done_event = win32event.CreateEvent(None, 0, 0, DONE_EVENT_NAME)

while True:
    win32event.SetEvent(ready_event)
    win32event.WaitForSingleObject(done_event, win32event.INFINITE)