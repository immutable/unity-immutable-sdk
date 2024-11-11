import os
import mailslurp_client
from mailslurp_client.api import InboxControllerApi, WaitForControllerApi
import re

INBOX_ID = "33f17f82-274b-4269-9ce6-c620e89fcd8d"
EMAIL = "user-33f17f82-274b-4269-9ce6-c620e89fcd8d@mailslurp.biz"

def get_mailslurp_client():
    configuration = mailslurp_client.Configuration()
    configuration.api_key['x-api-key'] = '***REMOVED***' #os.getenv('MAILSLURP_API_KEY')
    api_client = mailslurp_client.ApiClient(configuration)
    waitfor_controller = WaitForControllerApi(api_client)
    return waitfor_controller

def extract_otp_from_email(email_body):
    # Pattern to match 6-digit code in Passport emails
    pattern = r'<h1[^>]*>(\d{6})</h1>'
    match = re.search(pattern, email_body)
    if match:
        return match.group(1)
    return None

def fetch_code():
    waitfor_controller = get_mailslurp_client()
    email = waitfor_controller.wait_for_latest_email(inbox_id=INBOX_ID, timeout=30000, unread_only=True)
    if email:
        otp = extract_otp_from_email(email.body)
        return otp
    return None

if __name__ == "__main__":
    code = fetch_code()
    print(code)