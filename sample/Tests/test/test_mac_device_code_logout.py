import time
import unittest

from alttester import *

class MacTest(unittest.TestCase):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        cls.altdriver = AltDriver()

    @classmethod
    def tearDownClass(cls):
        cls.altdriver.stop()

    def test_5_logout(self):
        # Logout
        self.altdriver.find_object(By.NAME, "LogoutBtn").tap()

        time.sleep(10)

        # Wait for authenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")
