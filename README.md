connect-replay
==============

Replays DocuSign Connect XML messages from S3 to the specified endpoint.

## AppSettings
To run this app, you need the following in your AppSettings:

**Endpoint** - The URL to post the connect XML to.
**Bucket** - The S3 bucket name to look in for the XML logs.
**ConnectPrefix** - The prefix to look under in the bucket for the connect XML logs e.g. connect/.

## Running
You can either provide an exact filename for the connect log, which will be found in the location <bucket>/<connectprefix>/<filename> e.g.

```
81e23161-7691-4550-a92b-64829b2a006d.xml
```

Or, you can provide a start date and end date in the following format to get and POST all logs in the time range.

```
yyyy-MM-dd HH:mm
```

## How it works
The XML file is downloaded from Amazon S3 as a byte array, then sent via a HTTP POST request to the specified connect endpoint. Each POST attempt is written to the console with the failure or success status.

**You must have S3 permissions for the bucket that is being read from**