import pytest
from appium import webdriver
from alttester import *

@pytest.fixture(scope='function')
def setWebdriver(request, session_capabilities):
    session_capabilities["autoAcceptAlerts"] = True
    remoteURL = "https://hub.browserstack.com/wd/hub"
    driver = webdriver.Remote(remoteURL, session_capabilities)

    altdriver = AltDriver(timeout=120)

    request.instance.driver = driver
    request.instance.altdriver = altdriver

    request.node._driver = driver

    yield driver

    driver.quit()
    altdriver.stop()