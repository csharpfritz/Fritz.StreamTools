


stream of messages..
parse the message - find if it has a url - w/ suffix .png. jpg, jpeg, gif

new code inside your bot to find image URL

see code in :: MessageTest.cs
--------------


> ImageCommand : icommand
reg expression \s htpp ...... (.png)
- stack overflow for regex url finder
- capture the sender

if img found - send it to vision api
--  > conection to azure - send the url


get back a descs + tags

Photo Found: {file name}

post back to the stream - the photo desc 

