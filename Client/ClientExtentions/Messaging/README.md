The messagaging provides full functionallity for a chat application.

Supported things:
* Encrypted chats
* Group chats


## Message storage
Usually message storage is done outside the cloud itself locally. Messages are grouped by time in to buckets.
All messages have an unique messageid ((sender),idfromsender) and the `idfromsender` is based on the `DateTime.Now.Ticks` of the sender.
