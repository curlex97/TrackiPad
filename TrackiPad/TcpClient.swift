//
//  Connection.swift
//  TrackiPad
//
//  Created by Admin on 11.03.16.
//  Copyright © 2016 Admin. All rights reserved.
//

import UIKit
typealias Byte = UInt8

class TcpClient : NSObject, NSStreamDelegate {
    var isConnect : Bool = false
    let serverPort: UInt32 = 29330
        private var inputStream: NSInputStream!
    private var outputStream: NSOutputStream!
    
    func toByteArray<T>(var value: T) -> [Byte] {
        return withUnsafePointer(&value) {
            Array(UnsafeBufferPointer(start: UnsafePointer<Byte>($0), count: sizeof(T)))
        }
    }
    
    func fromByteArray<T>(value: [Byte], _: T.Type) -> T {
        return value.withUnsafeBufferPointer {
            return UnsafePointer<T>($0.baseAddress).memory
        }
    }


    
    
    
    func connect(id:Int32) {
        print("connecting...")
        
        let b = toByteArray(id)
        
        
        
        var serverAddress: String = String(b[0])
        serverAddress += "."
        serverAddress +=  String(b[1])
        serverAddress += "."
        serverAddress +=  String(b[2])
        serverAddress += "."
        serverAddress +=  String(b[3])
        
        
        var readStream:  Unmanaged<CFReadStream>?
        var writeStream: Unmanaged<CFWriteStream>?
        
        
        
        CFStreamCreatePairWithSocketToHost(nil,  serverAddress, self.serverPort, &readStream, &writeStream)
        
        // Documentation suggests readStream and writeStream can be assumed to
        // be non-nil. If you believe otherwise, you can test if either is nil
        // and implement whatever error-handling you wish.
        
        self.inputStream = readStream!.takeRetainedValue()
        self.outputStream = writeStream!.takeRetainedValue()
        
        self.inputStream.delegate = self
        self.outputStream.delegate = self
        
        self.inputStream.scheduleInRunLoop(NSRunLoop.currentRunLoop(), forMode: NSDefaultRunLoopMode)
        self.outputStream.scheduleInRunLoop(NSRunLoop.currentRunLoop(), forMode: NSDefaultRunLoopMode)
        
        self.inputStream.open()
        self.outputStream.open()
        
        self.isConnect = true
    }
    
    
    func write(string: String)
    {
        let data: NSData = string.dataUsingEncoding(NSUTF8StringEncoding)!
        outputStream.write(UnsafePointer<UInt8>(data.bytes), maxLength: data.length)
    }
    
    func stream(stream: NSStream, handleEvent eventCode: NSStreamEvent) {
        
        
        var buffer = [UInt8](count: 4096, repeatedValue: 0)
        var result: Int = 0
        if stream == inputStream{
            while (inputStream.hasBytesAvailable){
                result = inputStream.read(&buffer, maxLength: buffer.count)
            }
            _ = NSString(bytes: &buffer, length: result, encoding: NSUTF8StringEncoding)
            
            // print("input stream event")
            if !isConnect{isConnect = true}
            else
            {
                //write(output! as String)
            }
        }
            
        else if stream == outputStream{
            //print("output stream event")
        }
        
        
        switch (eventCode){
        case NSStreamEvent.OpenCompleted:
            NSLog("Stream opened")
            break
        case NSStreamEvent.HasBytesAvailable:
            NSLog("HasBytesAvailable")            
            break
        case NSStreamEvent.ErrorOccurred:
            NSLog("ErrorOccurred")
            break
        case NSStreamEvent.EndEncountered:
            NSLog("EndEncountered")
            break
        default:
            NSLog("unknown.")
        }
    }
}