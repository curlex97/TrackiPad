//
//  ViewController.swift
//  TrackiPad
//
//  Created by Admin on 12.03.16.
//  Copyright © 2016 Admin. All rights reserved.
//

import UIKit

class ViewController: UIViewController {

    @IBOutlet weak var connectTextField: UITextField!
    @IBOutlet weak var touchSlider: UISlider!
       @IBOutlet var rotationGesture: UIRotationGestureRecognizer!
    
    @IBOutlet var pinchGesture: UIPinchGestureRecognizer!
    
    @IBOutlet var longPressGesture: UILongPressGestureRecognizer!
    
    
    @IBOutlet weak var roundStyleTextField: UITextField!
    
    @IBOutlet weak var rotateSegment: UISegmentedControl!
    
    var rmode : String = "v_"
    
    let tcpClient : TcpClient = TcpClient()
    var touchPosition :[CGPoint] = [CGPoint(x: 1, y: 1)]
    
    override func viewDidLoad() {
        super.viewDidLoad()
        // Do any additional setup after loading the view, typically from a nib.
    }
    
    
    @IBAction func TapGestureAction(sender: AnyObject) {
        var msg : String = "p_"
        msg += String(self.touchPosition[0].x)
        msg += "_"
        msg += String(self.touchPosition[0].y)
        msg += "_e"
        tcpClient.write(msg)
    }
  
    @IBAction func RotationGestureRecognizer(sender: AnyObject) {
            var msg : String = rmode
            msg += String(rotationGesture.rotation)
            msg += "_e"
            tcpClient.write(msg)
  
    }
    
    
   
    @IBAction func longPressGestureAction(sender: AnyObject) {
        
        var msg : String = "l_"
        msg += String(self.touchPosition[0].x)
        msg += "_"
        msg += String(self.touchPosition[0].y)
        msg += "_e"
        tcpClient.write(msg)
        
    }
    
    @IBAction func pinchGestureRecognize(sender: AnyObject) {
      
            var msg : String = "i_"
            msg += String(pinchGesture.scale)
            msg += "_e"
            tcpClient.write(msg)
        
            
        
        
    }
    
    
    
    
    

    override func didReceiveMemoryWarning() {
        super.didReceiveMemoryWarning()
        // Dispose of any resources that can be recreated.
    }

    @IBAction func connectButtonOnTapped(sender: AnyObject) {
        
        let ip : Int32? = Int32(self.connectTextField.text!)
        
        if let _ = ip
        {
            if !tcpClient.isConnect
            {
                tcpClient.connect(ip!)
            }
        }
    }

    @IBAction func rotateSegmentOnTapped(sender: AnyObject) {
        if self.rotateSegment.selectedSegmentIndex == 0
        {
            rmode = "v_"
        }
        else
        {
            rmode = "b_"
        }
        
    }
   
    @IBAction func keyboardButtonOnTapped(sender: AnyObject) {
        self.roundStyleTextField.becomeFirstResponder()
    }
    
    
    @IBAction func roundStyleTextFieldEditingChanged(sender: AnyObject) {
        
        var msg : String = "k_"
        msg += String(self.roundStyleTextField.text)
        msg += "_e"
        self.roundStyleTextField.text = ""
        tcpClient.write(msg)
        
    }
    
    @IBAction func touchSliderOnTapped(sender: AnyObject) {
        var msg : String = "s_"
        msg += String(self.touchSlider.value)
        msg += "_e"
        tcpClient.write(msg)
        
    }
    
    override func touchesBegan(touches: Set<UITouch>, withEvent event: UIEvent?) {
        self.touchPosition.removeAll()
        
        for obj:AnyObject in touches {
            self.touchPosition += [obj.locationInView(view)]
        }
        super.touchesBegan(touches, withEvent:event)
        
    }
    
    override func touchesEnded(touches: Set<UITouch>, withEvent event: UIEvent?) {
        self.tcpClient.write("e_0_e");
    }
    
    
 
    
    
    override func touchesMoved(touches: Set<UITouch>, withEvent event: UIEvent? )
    {
        
        super.touchesMoved(touches, withEvent:event)
        
            self.touchPosition.removeAll()
        
        for obj:AnyObject in touches {
            self.touchPosition += [obj.locationInView(view)]
        }
        
        
            
                if(self.touchPosition.count == 1 && self.touchPosition[0].y > 82 && self.touchPosition[0].y < 684)
                {
                    var msg : String = "m_"
                    if self.touchPosition[0].x > 900
                    {
                        msg = "c_"
                    }
                    
                    
                    msg += String(self.touchPosition[0].x)
                    msg += "_"
                    msg += String(self.touchPosition[0].y)
                    msg += "_e"
                    tcpClient.write(msg)
                }
        
        
        
        
    }
}

