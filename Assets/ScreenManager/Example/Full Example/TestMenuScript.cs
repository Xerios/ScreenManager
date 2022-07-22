using UnityEngine;
using System.Collections;
using ScreenMgr;
using UnityEngine.SceneManagement;

public class TestMenuScript : MonoBehaviour {

    public ScreenManager screenmgr;

    public void OnEnable() {
        StartCoroutine(TestDelayedTutorial());
    }

	public void TestPopup () {
        int i=1;
        screenmgr.Show<Popup>("Alertbox").Message = "Test " + (i++);
        screenmgr.Show<Popup>("Alertbox").Message = "Test " + (i++);
        screenmgr.Show<Popup>("Alertbox").Message = "Test " + (i++);
        StartCoroutine(TestDelayedPopup());
    }


    public IEnumerator TestDelayedPopup() {
        yield return new WaitForSeconds(2f);
        screenmgr.Show<Popup>("Alertbox").Message = "W" +Random.value;
    }

    public IEnumerator TestDelayedTutorial() {
        yield return new WaitForSeconds(5f);
        screenmgr.Show<Popup>("Tutorial");
    }

    public void LoadGame() {
        //SceneManager.LoadScene("Game", LoadSceneMode.Additive);
        //screenmgr.gameObject.SetActive(false);
    }
}
