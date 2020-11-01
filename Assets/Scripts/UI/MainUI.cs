using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

public class MainUI : MonoBehaviour
{
  public GameObject matchPrefab, backButton, notSignedInGroup, newUserGroup, infoGroup, signedInGroup, incorrectLoginGroup;
  public Button refreshButton, resendEmailButton;
  public Text checkEmailInfo, signedInInfo;
  public InputField usernameField, passwordField,matchNameField, newUserEmailField, newUserPasswordField, newUserDisplayNameField;
  public Transform matchScrollview;
  NetworkClient network;
  EventSystem eventSystem;
  bool lockConnection, lockRefresh;
  public void Initialize()
  {
    network = GameManager.networkClient;
    eventSystem = EventSystem.current;

    if (!NetworkClient.IsLoggedIn){
      notSignedInGroup.SetActive(true);
    }
    else{
      SignedInWindow();
    }
  }

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.Tab))
    {
        Selectable next = eventSystem.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
        if (next!= null) {
                          
            InputField inputfield = next.GetComponent<InputField>();
            if (inputfield !=null) inputfield.OnPointerClick(new PointerEventData(eventSystem));  //if it's an input field, also set the text caret
                          
            eventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(eventSystem));
        }
        else Debug.Log("next nagivation element not found");
      
    }

    // Developer debug @TODO: remove!!!
    if (Input.GetKey(KeyCode.RightAlt) && Input.GetKey(KeyCode.Alpha0)){
      if (Input.GetKey(KeyCode.R)){
        lockConnection = false;
      }
    }
  }

  // ===== Views ======
  void LoginFailed(){
    incorrectLoginGroup.SetActive(true);
    backButton.SetActive(true);
    notSignedInGroup.SetActive(false);
  }
  void MainLogin(){
    if (!NetworkClient.IsLoggedIn) notSignedInGroup.SetActive(true);    // show login
    else SignedInWindow();  // show user info
    backButton.SetActive(false);  // return to toplevel menu
  }
  void SignedInWindow(){
    signedInGroup.SetActive(true);
    if (!NetworkClient.user.IsEmailVerified){
      signedInInfo.text = "Signed in as "+NetworkClient.user.DisplayName+"\nYou must verify your email before continuing.\nCan't find your email?";
      resendEmailButton.gameObject.SetActive(true);
    }
    else{
      resendEmailButton.gameObject.SetActive(false);
    }
  }
  void RegistrationResult(string result){
    infoGroup.SetActive(true);
    checkEmailInfo.text = result;
  }

  // ====== Buttons ======
  // Login
  public void OnPasswordEndEdit(){    // This is called by the password field losing focus
    if (Input.GetKey(KeyCode.Return))
      OnClickedLogin();
  }
  public async void OnClickedLogin(){
    string email = "";
    if (usernameField.text.Contains("@")) email = usernameField.text;
    else {
      // We must first get the email from the display name (if passwords match)
      User u = await NetworkClient.GetUserWithDisplayName(usernameField.text, passwordField.text);

      if (u == null){
        LoginFailed();
        return;
      }
      else
        email = u.email;
    }
    bool success = await NetworkClient.Login(email, passwordField.text);
    if (!success) LoginFailed();
    else{
      notSignedInGroup.SetActive(false);
      MainLogin();  // Should disable back button and run SignedInWindow();
    }
  }
  public void OnClickedLogout(){
    NetworkClient.auth.SignOut();
    signedInGroup.SetActive(false);
    MainLogin();
  }
  public void OnClickedResendEmail(){
    NetworkClient.user.SendEmailVerificationAsync();
    signedInInfo.text = "Verification email sent to "+NetworkClient.user.Email;
  }
  public void OnClickedNewUser(){
    notSignedInGroup.SetActive(false);
    newUserGroup.SetActive(true);
    backButton.SetActive(true);
  }
  public void OnClickedLoginBack(){
    // In registration form
    if (newUserGroup.activeSelf) newUserGroup.SetActive(false); // turn off registration form
    // Registration confirmation
    else if (infoGroup.activeSelf) infoGroup.SetActive(false);  // turn off registration confirmation
    else if (incorrectLoginGroup.activeSelf) incorrectLoginGroup.SetActive(false);
    MainLogin();
  }
  public async void OnClickedRegister(){
    if (newUserEmailField.text=="" || newUserPasswordField.text=="" || newUserDisplayNameField.text==""
      || newUserPasswordField.text.Contains("/") || newUserDisplayNameField.text.Contains("/"))
      return;
    if (!IsEmailAddress(newUserEmailField.text)){
      newUserGroup.SetActive(false);
      RegistrationResult("Not a valid email address.");
      return;
    }
    newUserGroup.SetActive(false);
    RegistrationResult("Processing");

    await Task.Run( () => NetworkClient.BeginRegistration(newUserEmailField.text,
      newUserPasswordField.text, newUserDisplayNameField.text) )
      .ContinueWith(task => MainThread.wkr.AddJob(()=>
      {
        switch (task.Result)
        {
          case "success":
            RegistrationResult("Credentials submitted.\nPlease check your email to finish creating your account.");
          break;
          case "name taken":
            RegistrationResult("That Display Name is taken.");
          break;
          case "email in use":
            RegistrationResult("That email address is already in use.");
          break;
          default:
            RegistrationResult("Unknown error.");
          break;
        }
      }));
  }

  // Lobby
  public void OnClickedNewMatch(){  // Inspector assigned action
    if (matchNameField.textComponent.text == "")
      return;

    OnClickedNewMatch(matchNameField.textComponent.text);
  }
  void OnClickedNewMatch(string s)  // listener added action
  {
    if (lockConnection)
      return;

    lockConnection = true;
    network.StartNewMatch(s);
    // @TODO: deselct, close menu
  }

  public async void OnClickedRefresh(){
    if (lockRefresh)
      return;
    
    lockRefresh = true;
    foreach (Transform c in matchScrollview){    // delete previous list
      GameObject.Destroy(c.gameObject);
    }
    List<Match> matches = await network.GetMatchList();
    for (int i=0; i<matches.Count; i++){
      GameObject g = GameObject.Instantiate(matchPrefab);//, Vector3.up*80, Quaternion.identity, matchScrollview);
      g.transform.SetParent(matchScrollview, false);
      g.transform.localPosition = Vector3.up * i * -30;
      g.transform.SetAsFirstSibling();
      g.transform.Find("Name").GetComponent<Text>().text = matches[i].name;
      g.transform.Find("Type").GetComponent<Text>().text = "Type: "+matches[i].type;
      g.transform.Find("Players").GetComponent<Text>().text = matches[i].players+"/6";
      string id = matches[i].id;  // This must be accessed and evaluated now, not in the lambda exp.
      g.transform.Find("Join Button").GetComponent<Button>().onClick.AddListener( ()=>{
        network.JoinMatch(id);
        // @TODO: deselect, close menu
      });
    }
    lockRefresh = false;
  }

  // ======== Tools ===========
  bool IsEmailAddress(string email){
    Regex regex = new Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase);
    return regex.IsMatch(email);
  }
}
