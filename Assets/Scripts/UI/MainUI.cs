using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainUI : MonoBehaviour
{
  public GameObject host_joinPanel, chatPanel;
  public RectTransform chatAnchorTrans;
  public Button dummyButton;
  public InputField chatField;
  public Text chatmessagePrefab;
  Animator host_joinAnim, chatAnim;
  bool chatOpen;
  float chatScrollOffset = -5f;
  
  private void Start() {
    host_joinAnim = host_joinPanel.GetComponent<Animator>();
    chatAnim = chatPanel.GetComponent<Animator>();
  }
  
  public void OnChatReceived(string msg){
    Text newMsg = Instantiate(chatmessagePrefab, chatAnchorTrans);
    newMsg.transform.SetParent(chatAnchorTrans);
    newMsg.transform.localPosition = Vector3.up * chatScrollOffset;    // Seems like it just needs a 20pt offset idk
    chatScrollOffset -= 5;
    chatAnchorTrans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, chatScrollOffset *-1);
    newMsg.text = msg;
  }

  void Update(){
    if (Input.GetKeyDown(KeyCode.Return)){
      if (!chatOpen){   // Open the chat window
        chatOpen = true;
        chatAnim.SetBool("chat open", true);
        chatField.Select();
      }
      else if (EventSystem.current.currentSelectedGameObject != chatField.gameObject){   
        // Select chat bar
        chatField.Select();
      }
      else{
        // Send chat message
        GameManager.networkManager.SendChat(chatField.text);
        chatField.text = "";
      }
    }

    if (Input.GetKeyDown(KeyCode.Escape)){
      if (chatOpen){
        if (EventSystem.current.currentSelectedGameObject == chatField.gameObject)
        {
          chatField.text = "";
          dummyButton.Select();
        }
        else{
          chatOpen = false;
          chatAnim.SetBool("chat open", false);
        }
      }
    }
  }

  public void OnClickJoin(Text text){  // Called by Join Button gameobject
    GameManager.networkManager.OnJoinServer(text.text!=""?text.text:"ws://localhost:2567/");
    host_joinAnim.SetBool("host-join open", false);
    dummyButton.Select();   // Prevents accidental clicking by pressing enter. May need to disable gameobject instead
  }
  
  public void OnHostServer(Text text){  // Called by Host Button gameobject
    GameManager.networkManager.OnHostServer(text.text!=""?text.text:"ws://localhost:2567/");
    host_joinAnim.SetBool("host-join open", false);
    dummyButton.Select();
  }

}
