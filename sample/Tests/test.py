import unittest
import os
from alttester import *

class UnityTest(unittest.TestCase):

    altdriver = None

    @classmethod
    def setUpClass(cls):
        host = os.getenv('ALTSERVER_HOST', '127.0.0.1')
        port = int(os.getenv('ALTSERVER_PORT', '13000'))
        cls.altdriver = AltDriver(host=host, port=port)

    @classmethod
    def tearDownClass(cls):
        cls.altdriver.stop()

    def test(self):
        # Select use device code auth
        self.altdriver.find_object(By.NAME, "DeviceCodeAuth").tap()

        # Wait for unauthenticated screen
        self.altdriver.wait_for_current_scene_to_be("UnauthenticatedScene")

        assert True