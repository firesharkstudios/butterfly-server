read -p "This will modify *.cs files recursively starting in the current directory. You should backup your files before running this. Proceed (y/N)? " -n 1 -r
echo    # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Core.Auth/Butterfly.Auth/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Core.Util/Butterfly.Util/g' {} \;

	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Core.Notify/Butterfly.Message/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Aws/Butterfly.Message.Aws/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Twilio/Butterfly.Message.Twilio/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/NotifyManager/SendMessageQueueManager/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/notifyManager/sendMessageQueueManager/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/NotifyMessage/SendMessage/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/notifyMessage/sendMessage/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/AwsSesEmailSendMessageSender/AwsSesEmailMessageSender/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/TwilioPhoneSendMessageSender/TwilioTextMessageSender/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/emailSendMessageSender:/emailMessageSender:/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/phoneSendMessageSender:/textMessageSender:/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/ISendMessageSender/IMessageSender/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/AwsModule/AwsWebApi/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/TwilioModule/TwilioWebApi/g' {} \; 
	find . -type f -name '*.cs' -exec sed -i -e 's/BaseWebApi.FileUploadHandlerAsync/WebApiUtil.FileUploadHandlerAsync/g' {} \; 

	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Core.Database/Butterfly.Db/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.MySql/Butterfly.Db.MySql/g' {} \;

	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Core.WebApi/Butterfly.Web.WebApi/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.Core.Channel/Butterfly.Web.Channel/g' {} \;
	find . -type f -name '*.cs' -exec sed -i -e 's/Butterfly.EmbedIO/Butterfly.Web.EmbedIO/g' {} \;
fi
