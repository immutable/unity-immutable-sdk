
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys

def main():
    print("Connect to Chrome")
    # Set up Chrome options to connect to the existing Chrome instance
    chrome_options = Options()
    chrome_options.add_experimental_option("debuggerAddress", "localhost:9222")
    # Connect to the existing Chrome instance
    driver = webdriver.Chrome(options=chrome_options)

    print("Waiting for new window...")
    WebDriverWait(driver, 60).until(EC.number_of_windows_to_be(2))

    # Get all window handles
    all_windows = driver.window_handles

    print("Find the new window")
    new_window = [window for window in all_windows if window != driver.current_window_handle][0]

    print("Switch to the new window")
    driver.switch_to.window(new_window)

    driver.quit()

if __name__ == "__main__":
    main()