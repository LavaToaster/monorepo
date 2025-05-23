//
//  ImageViewerView.swift
//  ImmichLens
//
//  Created on 10/05/2025.
//

import SwiftUI
import os

struct ImageViewerView: View {
  @EnvironmentObject var apiService: APIService
  let asset: Asset
  
  private let logger = Logger(subsystem: "dev.lav.immichlens", category: "ImageViewerView")

  var body: some View {
    ZStack {
      Color.black.edgesIgnoringSafeArea(.all)
      
      AsyncImage(
        url: asset.imageUrl(size: .preview)
      ) { phase in
        switch phase {
        case .empty:
          ProgressView()
            .foregroundColor(.white)
        case .success(let image):
          image
            .resizable()
            .scaledToFit()
            .frame(maxWidth: .infinity, maxHeight: .infinity)
            .edgesIgnoringSafeArea(.all)
        case .failure(let error):
          VStack {
            Image(systemName: "photo")
              .font(.system(size: 70))
              .foregroundColor(.gray)
            
            Text(error.localizedDescription)
              .font(.caption)
              .multilineTextAlignment(.center)
              .padding()
              .foregroundColor(.gray)
          }
          .onAppear {
            logger.error("Failed to load image: \(error.localizedDescription)")
          }
        @unknown default:
          EmptyView()
        }
      }
      .focusable()
    }
    .ignoresSafeArea()
  }
}
