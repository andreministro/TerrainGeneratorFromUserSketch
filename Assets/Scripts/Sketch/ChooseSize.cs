using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChooseSize : MonoBehaviour
{
    public void Choose32()
    {
        SceneManager.LoadScene(1);
    }

    public void Choose64()
    {
        SceneManager.LoadScene(2);
    }

    public void Choose128()
    {
        SceneManager.LoadScene(3);
    }

    public void Choose256()
    {
        SceneManager.LoadScene(4);
    }

    public void Choose512()
    {
        SceneManager.LoadScene(5);
    }

    public void Choose1024()
    {
        SceneManager.LoadScene(6);
    }
}
