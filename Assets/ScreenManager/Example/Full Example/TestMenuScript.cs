using UnityEngine;
using System.Collections;
using ScreenMgr;

public class TestMenuScript : MonoBehaviour {
    public void OnEnable() {
        StartCoroutine(TestDelayedTutorial());
    }

	public void TestPopup () {
        int i=1;
        ScreenManager.Instance.Show<Popup>("Alertbox").Message = "Test " + (i++);
        ScreenManager.Instance.Show<Popup>("Alertbox").Message = "Test " + (i++);
        ScreenManager.Instance.Show<Popup>("Alertbox").Message = "Test " + (i++);
        StartCoroutine(TestDelayedPopup());
    }


    public IEnumerator TestDelayedPopup() {
        yield return new WaitForSeconds(2f);
        ScreenManager.Instance.Show<Popup>("Alertbox").Message = "W" +Random.value;
    }

    public IEnumerator TestDelayedTutorial() {
        yield return new WaitForSeconds(5f);
        ScreenManager.Instance.Show<Popup>("Tutorial");
    }

    public void LoadGame() {
        //SceneManager.LoadScene("Game", LoadSceneMode.Additive);
        //screenmgr.gameObject.SetActive(false);
    }
}
