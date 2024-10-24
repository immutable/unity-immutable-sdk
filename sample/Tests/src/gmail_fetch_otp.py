import os.path
import re
import base64
from google.auth.transport.requests import Request
from google.oauth2.credentials import Credentials
from google_auth_oauthlib.flow import InstalledAppFlow
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError

SCOPES = ["https://www.googleapis.com/auth/gmail.readonly"]

def fetch_gmail_code():
    """Fetches the latest email from 'hello@passport.e.immutable.com' and returns a 6-digit code."""

    creds = None
    # The file token.json stores the user's access and refresh tokens, and is
    # created automatically when the authorization flow completes for the first
    # time.
    if os.path.exists("token.json"):
        creds = Credentials.from_authorized_user_file("token.json", SCOPES)
    # If there are no (valid) credentials available, let the user log in.
    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            creds.refresh(Request())
        else:
            flow = InstalledAppFlow.from_client_secrets_file(
                "credentials.json", SCOPES
            )
            creds = flow.run_local_server(port=0)
        # Save the credentials for the next run
        with open("token.json", "w") as token:
            token.write(creds.to_json())

    try:
        # Call the Gmail API
        service = build("gmail", "v1", credentials=creds)

        # Fetch the latest email from 'hello@passport.e.immutable.com'
        results = service.users().messages().list(userId="me", q="from:hello@passport.e.immutable.com", maxResults=1).execute()
        messages = results.get("messages", [])

        if not messages:
            print("No messages found.")
            return None

        # Get the ID of the latest message
        latest_message_id = messages[0]["id"]

        # Retrieve the full message details
        message = service.users().messages().get(userId="me", id=latest_message_id).execute()

        # Extract the 6-digit code from the email content
        msg_payload = message["payload"]
        if "parts" in msg_payload:
            for part in msg_payload["parts"]:
                if part["mimeType"] == "text/plain":
                    email_body = part["body"]["data"]
                    email_body_decoded = base64.urlsafe_b64decode(email_body).decode("utf-8")
                    code_match = re.search(r"\b\d{6}\b", email_body_decoded)
                    if code_match:
                        six_digit_code = code_match.group(0)
                        return six_digit_code
        else:
            print("No parts found in the email.")
            return None

    except HttpError as error:
        print(f"An error occurred: {error}")
        return None
